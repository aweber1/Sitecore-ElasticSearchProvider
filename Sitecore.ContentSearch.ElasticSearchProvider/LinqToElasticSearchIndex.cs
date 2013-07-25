using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Nest;
using Newtonsoft.Json;
using Sitecore.Configuration;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.ElasticSearchProvider.Linq;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Methods;
using Sitecore.ContentSearch.Linq.Nodes;
using Sitecore.ContentSearch.Pipelines.ProcessFacets;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Diagnostics;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class LinqToElasticSearchIndex<TItem> : ElasticSearchIndex<TItem>
	{
		// Fields
		private readonly ElasticSearchContext _context;
		private readonly string _cultureCode;

		public LinqToElasticSearchIndex(ElasticSearchContext context, IExecutionContext executionContext) : base(new ElasticSearchIndexParameters(context.Index.Configuration.IndexFieldStorageValueFormatter, context.Index.Configuration.VirtualFieldProcessors, context.Index.FieldNameTranslator, executionContext))
		{
			Assert.ArgumentNotNull(context, "context");
			_context = context;
			var cultureContext = Parameters.ExecutionContext as CultureExecutionContext;
			var culture = cultureContext == null ? CultureInfo.GetCultureInfo(Settings.DefaultLanguage) : cultureContext.Culture;
			_cultureCode = culture.TwoLetterISOLanguageName;
			//((ElasticSearchFieldNameTranslator) Parameters.FieldNameTranslator).AddCultureContext(culture);
		}

		private TResult ApplyScalarMethods<TResult, TDocument>(ElasticSearchQuery query, ElasticSearchResults<TDocument> processedResults)
		{
			object scalarResult;
			var method = query.Methods.First();
			switch (method.MethodType)
			{
				case QueryMethodType.All:
					scalarResult = true;
					break;

				case QueryMethodType.Any:
					scalarResult = processedResults.Any();
					break;

				case QueryMethodType.Count:
					scalarResult = processedResults.Count();
					break;

				case QueryMethodType.ElementAt:
					if (!((ElementAtMethod)method).AllowDefaultValue)
					{
						scalarResult = processedResults.ElementAt(((ElementAtMethod)method).Index);
					}
					else
					{
						scalarResult = processedResults.ElementAtOrDefault(((ElementAtMethod)method).Index);
					}
					break;

				case QueryMethodType.First:
					if (!((FirstMethod)method).AllowDefaultValue)
					{
						scalarResult = processedResults.First();
					}
					else
					{
						scalarResult = processedResults.FirstOrDefault();
					}
					break;

				case QueryMethodType.Last:
					if (!((LastMethod)method).AllowDefaultValue)
					{
						scalarResult = processedResults.Last();
					}
					else
					{
						scalarResult = processedResults.LastOrDefault();
					}
					break;

				case QueryMethodType.Single:
					if (!((SingleMethod)method).AllowDefaultValue)
					{
						scalarResult = processedResults.Single();
					}
					else
					{
						scalarResult = processedResults.SingleOrDefault();
					}
					break;

				case QueryMethodType.GetResults:
					{
						var searchHits = processedResults.GetSearchHits();
						var results2 = FormatFacetResults(processedResults.GetFacets(), query.FacetQueries);
						scalarResult = Activator.CreateInstance(typeof(TResult), new object[] { searchHits, processedResults.NumberFound, results2 });
						break;
					}
				case QueryMethodType.GetFacets:
					scalarResult = FormatFacetResults(processedResults.GetFacets(), query.FacetQueries);
					break;

				default:
					throw new InvalidOperationException("Invalid query method");
			}

			return (TResult)System.Convert.ChangeType(scalarResult, typeof(TResult));
		}

		public override TResult Execute<TResult>(ElasticSearchQuery query)
		{
			if (typeof(TResult).IsGenericType && (typeof(TResult).GetGenericTypeDefinition() == typeof(SearchResults<>)))
			{
				var resultType = typeof(TResult).GetGenericArguments()[0];
				var results = Execute(query, resultType);

				var searchResultsType = typeof(ElasticSearchResults<>).MakeGenericType(new[] { resultType });
				var applyScalarMethods = GetType().GetMethod("ApplyScalarMethods", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new[] { typeof(TResult), resultType });

				var method = GetSelectMethod(query);
				var searchResultsInstance = Activator.CreateInstance(searchResultsType, new object[] { _context, results, method, query.VirtualFieldProcessors });

				return (TResult)applyScalarMethods.Invoke(this, new[] { query, searchResultsInstance });
			}

			var searchResults = Execute(query, typeof(TResult));
			var selectMethod = GetSelectMethod(query);
			var processedResults = new ElasticSearchResults<TResult>(_context, searchResults, selectMethod, query.VirtualFieldProcessors);

			return ApplyScalarMethods<TResult, TResult>(query, processedResults);
		}

		internal IQueryResponse<Dictionary<string, object>> Execute(ElasticSearchQuery query, Type resultType)
		{
			var index = _context.Index as ElasticSearchIndex;
			if (index == null)
				return new QueryResponse<Dictionary<string, object>>();

			var descriptor = new SearchDescriptor<Dictionary<string, object>>();
			descriptor.Query(query.Query);

			//TODO: need to determine how to allow type specification, so that we don't have to hard-code AllTypes
			//NEST generates the search query URL based on the descriptor type, so if we use Dictionary<string, object> as the descriptor type, the query URL is:
			//http://[server]:9200/[indexname]/dictionary`2s/_search
			//instead we want it to be:
			//http://[server]:9200/[indexname]/_search 
			//when searching over all types. so basically, we want the default to AllTypes unless a specific type is used. 
			//In the context of Sitecore search, is it even possible to specify a type for the search? probably, but would need some sort of type mapping...
			descriptor.AllTypes(); 
			
			if (query.Filter != null)
			{
				//TODO: i would be amazed if this actually works... more likely need a way to make query.Filter a BaseFilter as opposed to BaseQuery
				//update: this actually does work, and i am amazed, as expected.
				descriptor.Filter(filterDescriptor => filterDescriptor.Query(q => query.Filter));
			}

			if (!Settings.DefaultLanguage.StartsWith(_cultureCode))
			{
				descriptor.Filter(f => f.Query(q => q.Term("_language", _cultureCode)));
			}

			var isResultsSizeSet = false;
			if (query.Methods != null)
			{
				var fields = new List<string>();

				var selectMethods = (from m in query.Methods
									 where m.MethodType == QueryMethodType.Select
									 select (SelectMethod)m).ToList<SelectMethod>();

				if (selectMethods.Any())
				{
					foreach (var method in selectMethods)
					{
						fields.AddRange(method.FieldNames.Select(fieldName => fieldName.ToLowerInvariant()));
					}

					if (!_context.SecurityOptions.HasFlag(SearchSecurityOptions.DisableSecurityCheck))
					{
						fields.Add("_uniqueid");
						fields.Add("_datasource");
					}
				}

				var getResultsMethods = (from m in query.Methods
										 where m.MethodType == QueryMethodType.GetResults
										 select (GetResultsMethod)m).ToList<GetResultsMethod>();

				if (getResultsMethods.Any())
				{
					if (fields.Count > 0)
					{
						fields.Add("score");
					}
				}
				
				if (fields.Count > 0)
					descriptor.Fields(fields.ToArray());

				var orderByMethods = (from m in query.Methods
									  where m.MethodType == QueryMethodType.OrderBy
									  select (OrderByMethod)m).ToList<OrderByMethod>();

				if (orderByMethods.Any())
				{
					foreach (var method in orderByMethods)
					{
						var fieldName = method.Field;
						switch (method.SortDirection)
						{
							case SortDirection.Ascending:
								descriptor.SortAscending(fieldName);
								break;
							case SortDirection.Descending:
								descriptor.SortDescending(fieldName);
								break;
						}
					}
				}

				var skipMethods = (from m in query.Methods
								   where m.MethodType == QueryMethodType.Skip
								   select (SkipMethod)m).ToList<SkipMethod>();

				if (skipMethods.Any())
				{
					var num = skipMethods.Sum(skipMethod => skipMethod.Count);
					descriptor.Skip(num);
				}

				var takeMethods = (from m in query.Methods
								   where m.MethodType == QueryMethodType.Take
								   select (TakeMethod)m).ToList<TakeMethod>();

				if (takeMethods.Any())
				{
					var num2 = takeMethods.Sum(takeMethod => takeMethod.Count);
					descriptor.Size(num2); //Take is actually just an alias for Size in NEST, so just use Size instead.
					isResultsSizeSet = true; 
				}

				var countMethods = (from m in query.Methods
									where m.MethodType == QueryMethodType.Count
									select (CountMethod)m).ToList<CountMethod>();

				if (query.Methods.Count == 1 && countMethods.Any())
				{
					descriptor.Size(0); //TODO: is using Size appropriate here? and is "0" the proper value to send?
					isResultsSizeSet = true;
				}

				var anyMethods = (from m in query.Methods
								  where m.MethodType == QueryMethodType.Any
								  select (AnyMethod)m).ToList<AnyMethod>();

				if (query.Methods.Count == 1 && anyMethods.Any())
				{
					descriptor.Size(0); //TODO: is using Size appropriate here? and is "0" the proper value to send?
					isResultsSizeSet = true;
				}

				var getFacetsMethods = (from m in query.Methods
										where m.MethodType == QueryMethodType.GetFacets
										select (GetFacetsMethod)m).ToList<GetFacetsMethod>();

				//TODO: implement facet querying
				if ((query.FacetQueries.Count > 0) && (getFacetsMethods.Any() || getResultsMethods.Any()))
				{
					//foreach (var facetQuery in GetFacetsPipeline.Run(new GetFacetsArgs(null, query.FacetQueries, _context.Index.Configuration.VirtualFieldProcessors, _context.Index.FieldNameTranslator)).FacetQueries.ToHashSet())
					//{
					//	if (!facetQuery.FieldNames.Any())
					//		continue;

					//	var nullable = facetQuery.MinimumResultCount;
					//	if (facetQuery.FieldNames.Count() == 1)
					//	{
					//		var fieldNameTranslator = FieldNameTranslator as ElasticSearchFieldNameTranslator;
					//		var indexFieldName = facetQuery.FieldNames.First();
					//		if (((fieldNameTranslator != null) && (indexFieldName == fieldNameTranslator.StripKnownExtensions(indexFieldName))) && (_context.Index.Configuration.FieldMap.GetFieldConfiguration(indexFieldName) == null))
					//		{
					//			indexFieldName = fieldNameTranslator.GetIndexFieldName(indexFieldName.Replace("__", "!").Replace("_", " ").Replace("!", "__"), true);
					//		}
					//		IElasticSearchFacetQuery[] queries = new IElasticSearchFacetQuery[1];
					//		ElasticSearchFacetFieldQuery query2 = new ElasticSearchFacetFieldQuery(indexFieldName)
					//			{
					//				MinCount = nullable
					//			};
					//		queries[0] = query2;
					//		options.AddFacets(queries);
					//	}
					//	if (facetQuery.FieldNames.Any())
					//	{
					//		IElasticSearchFacetQuery[] queryArray2 = new IElasticSearchFacetQuery[1];
					//		ElasticSearchFacetPivotQuery query3 = new ElasticSearchFacetPivotQuery
					//			{
					//				Fields = new[] { string.Join(",", facetQuery.FieldNames) },
					//				MinCount = nullable
					//			};
					//		queryArray2[0] = query3;
					//		options.AddFacets(queryArray2);
					//	}
					//}
				}
			}

			if (!isResultsSizeSet)
			{
				descriptor.Size(ContentSearchConfigurationSettings.SearchMaxResults);
			}

			//var blee = JsonConvert.SerializeObject(descriptor, Formatting.Indented);

			var serializedDescriptor = index.Client.Serialize(descriptor);
			SearchLog.Log.Info("Serialized Query - " + serializedDescriptor);
			
			var response = index.Client.Search(descriptor);
			if (!response.ConnectionStatus.Success)
			{
				SearchLog.Log.Error("Query exception - " + response.ConnectionStatus.Error.OriginalException);	
			}
			
			return response;
		}

		public override IEnumerable<TElement> FindElements<TElement>(ElasticSearchQuery compositeQuery)
		{
			var searchResults = Execute(compositeQuery, typeof(TElement));
			var list = (from m in compositeQuery.Methods
						where m.MethodType == QueryMethodType.Select
						select (SelectMethod)m).ToList<SelectMethod>();

			var selectMethod = (list.Count() == 1) ? list[0] : null;

			var results2 = new ElasticSearchResults<TElement>(_context, searchResults, selectMethod, compositeQuery.VirtualFieldProcessors);
			return results2.GetSearchResults();
		}

		private FacetResults FormatFacetResults(Dictionary<string, ICollection<KeyValuePair<string, int>>> facetResults, List<FacetQuery> facetQueries)
		{
			var fieldNameTranslator = _context.Index.FieldNameTranslator as ElasticSearchFieldNameTranslator;
			var dictionary = ProcessFacetsPipeline.Run(new ProcessFacetsArgs(facetResults, facetQueries, facetQueries, _context.Index.Configuration.VirtualFieldProcessors, fieldNameTranslator));
			using (var enumerator = facetQueries.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					var originalQuery = enumerator.Current;
					if (originalQuery == null || originalQuery.FilterValues == null || !originalQuery.FilterValues.Any() || !dictionary.ContainsKey(originalQuery.CategoryName))
						continue;

					var is2 = dictionary[originalQuery.CategoryName];
					Func<KeyValuePair<string, int>, bool> func = cv => originalQuery.FilterValues.Contains(cv.Key);

					dictionary[originalQuery.CategoryName] = is2.Where(func).ToList();
				}
			}

			var results = new FacetResults();
			foreach (var pair in dictionary)
			{
				if (fieldNameTranslator == null)
					continue;

				var key = pair.Key;
				if (key.Contains(","))
				{
					key = fieldNameTranslator.StripKnownExtensions(key.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries));
				}
				else
				{
					key = fieldNameTranslator.StripKnownExtensions(key);
				}

				var enumerable = from v in pair.Value select new FacetValue(v.Key, v.Value);
				results.Categories.Add(new FacetCategory(key, enumerable));
			}
			return results;
		}

		private static SelectMethod GetSelectMethod(ElasticSearchQuery compositeQuery)
		{
			var list = (from m in compositeQuery.Methods
						where m.MethodType == QueryMethodType.Select
						select (SelectMethod)m).ToList<SelectMethod>();

			return list.Count != 1 ? null : list[0];
		}
	}
}

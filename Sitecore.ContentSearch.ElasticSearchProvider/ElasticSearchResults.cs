using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Nest;
using Sitecore.ContentSearch.ElasticSearchProvider.Mapping;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Methods;
using Sitecore.ContentSearch.Pipelines.IndexingFilters;
using Sitecore.ContentSearch.Security;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct ElasticSearchResults<TElement>
	{
		private readonly ElasticSearchContext _context;
		private readonly IQueryResponse<Dictionary<string, object>> _searchResults;
		private readonly ElasticSearchIndexConfiguration _elasticSearchIndexConfiguration;
		private readonly ElasticSearchDocumentPropertyMapper _mapper;
		private readonly SelectMethod _selectMethod;
		private readonly IEnumerable<IFieldQueryTranslator> _virtualFieldProcessors;
		private readonly int _resultsTotal;

		public ElasticSearchResults(ElasticSearchContext context, IQueryResponse<Dictionary<string, object>> searchResults, SelectMethod selectMethod, IEnumerable<IFieldQueryTranslator> virtualFieldProcessors)
		{
			_context = context;
			_elasticSearchIndexConfiguration = (ElasticSearchIndexConfiguration)_context.Index.Configuration;
			_mapper = (ElasticSearchDocumentPropertyMapper)_elasticSearchIndexConfiguration.IndexDocumentPropertyMapper;
			_selectMethod = selectMethod;
			_virtualFieldProcessors = virtualFieldProcessors;
			_resultsTotal = searchResults.Total;
			_searchResults = ApplySecurity(searchResults, context.SecurityOptions, ref _resultsTotal);
		}

		public int NumberFound
		{
			get { return _resultsTotal; }
		}

		private static IQueryResponse<Dictionary<string, object>> ApplySecurity(IQueryResponse<Dictionary<string, object>> queryResults, SearchSecurityOptions options, ref int resultsTotal)
		{
			if (!options.HasFlag(SearchSecurityOptions.DisableSecurityCheck))
			{
				var hitsToRemove = new HashSet<IHit<Dictionary<string, object>>>();
				foreach (var hit in from searchResult in queryResults.Hits.Hits
										   where searchResult != null
										   select searchResult)
				{
					object uniqueId;
					if (!hit.Source.TryGetValue("_uniqueid", out uniqueId)) //TODO: shouldn't have to use the Source property here, the Fields property should be populated. probably something wrong with field mapping.
						continue;

					object datasource;
					hit.Source.TryGetValue("_datasource", out datasource); //TODO: shouldn't have to use the Source property here, the Fields property should be populated. probably something wrong with field mapping.
					if (!OutboundIndexFilterPipeline.CheckItemSecurity(new OutboundIndexFilterArgs((string) uniqueId, (string) datasource))) 
						continue;

					hitsToRemove.Add(hit);
				}

				foreach (var hit in hitsToRemove)
				{
					queryResults.Hits.Hits.Remove(hit);
					resultsTotal--;
				}
			}
			return queryResults;
		}

		public TElement ElementAt(int index)
		{
			if ((index < 0) || (index > _searchResults.Total))
			{
				throw new IndexOutOfRangeException();
			}
			return _mapper.MapToType<TElement>(_searchResults.Documents.ElementAt(index), _selectMethod, _virtualFieldProcessors, _context.SecurityOptions);
		}

		public TElement ElementAtOrDefault(int index)
		{
			if ((index >= 0) && (index <= _searchResults.Total))
			{
				return _mapper.MapToType<TElement>(_searchResults.Documents.ElementAt(index), _selectMethod, _virtualFieldProcessors, _context.SecurityOptions);
			}
			return default(TElement);
		}

		public bool Any()
		{
			return _resultsTotal > 0;
		}

		public long Count()
		{
			return _resultsTotal;
		}

		public TElement First()
		{
			if (_searchResults.Total < 1)
			{
				throw new InvalidOperationException("Sequence contains no elements");
			}
			return ElementAt(0);
		}

		public TElement FirstOrDefault()
		{
			if (_searchResults.Total < 1)
			{
				return default(TElement);
			}
			return ElementAt(0);
		}

		public TElement Last()
		{
			if (_searchResults.Total < 1)
			{
				throw new InvalidOperationException("Sequence contains no elements");
			}
			return ElementAt(_searchResults.Total - 1);
		}

		public TElement LastOrDefault()
		{
			return _searchResults.Total < 1 ? default(TElement) : ElementAt(_searchResults.Total - 1);
		}

		public TElement Single()
		{
			if (_resultsTotal < 1)
			{
				throw new InvalidOperationException("Sequence contains no elements");
			}
			if (_resultsTotal > 1)
			{
				throw new InvalidOperationException("Sequence contains more than one element");
			}
			return _mapper.MapToType<TElement>(_searchResults.Documents.ElementAt(0), _selectMethod, _virtualFieldProcessors, _context.SecurityOptions);
		}

		public TElement SingleOrDefault()
		{
			if (_resultsTotal != 1L)
			{
				return default(TElement);
			}
			return _mapper.MapToType<TElement>(_searchResults.Documents.ElementAt(0), _selectMethod, _virtualFieldProcessors, _context.SecurityOptions);
		}

		public IEnumerable<SearchHit<TElement>> GetSearchHits()
		{
			foreach (var doc in _searchResults.Documents)
			{
				object rawScore;
				var score = -1f;
				if (doc.TryGetValue("score", out rawScore) && (rawScore is float))
				{
					score = (float)rawScore;
				}
				yield return new SearchHit<TElement>(score, _mapper.MapToType<TElement>(doc, _selectMethod, _virtualFieldProcessors, _context.SecurityOptions));
			}
		}

		public IEnumerable<TElement> GetSearchResults()
		{
			foreach (var doc in _searchResults.Documents)
			{
				yield return _mapper.MapToType<TElement>(doc, _selectMethod, _virtualFieldProcessors, _context.SecurityOptions);
			}
		}

		//TODO: implement this method properly
		public Dictionary<string, ICollection<KeyValuePair<string, int>>> GetFacets()
		{
			//IDictionary<string, ICollection<KeyValuePair<string, int>>> facetFields = (IDictionary<string, ICollection<KeyValuePair<string, int>>>)this._searchResults.FacetFields;
			//IDictionary<string, IList<Pivot>> facetPivots = (IDictionary<string, IList<Pivot>>)this._searchResults.FacetPivots;
			//Dictionary<string, ICollection<KeyValuePair<string, int>>> dictionary3 = facetFields.ToDictionary<KeyValuePair<string, ICollection<KeyValuePair<string, int>>>, string, ICollection<KeyValuePair<string, int>>>(x => x.Key, x => x.Value);
			//if (facetPivots.Count > 0)
			//{
			//	foreach (KeyValuePair<string, IList<Pivot>> pair in facetPivots)
			//	{
			//		dictionary3.set_Item(pair.Key, this.Flatten(pair.Value, string.Empty));
			//	}
			//}
			//return dictionary3;
			return new Dictionary<string, ICollection<KeyValuePair<string, int>>>();
		}

		
		//private ICollection<KeyValuePair<string, int>> Flatten(IEnumerable<Pivot> pivots, string parentName)
		//{
		//	HashSet<KeyValuePair<string, int>> set = new HashSet<KeyValuePair<string, int>>();
		//	foreach (Pivot pivot in pivots)
		//	{
		//		if (parentName != string.Empty)
		//		{
		//			set.Add(new KeyValuePair<string, int>(parentName + "/" + pivot.Value, pivot.Count));
		//		}
		//		if (pivot.HasChildPivots)
		//		{
		//			set.UnionWith(this.Flatten((IEnumerable<Pivot>)pivot.ChildPivots, pivot.Value));
		//		}
		//	}
		//	return set;
		//}


	}



}

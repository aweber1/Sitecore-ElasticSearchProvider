using System;
using System.Collections.Generic;
using Nest;
using Sitecore.ContentSearch.ElasticSearchProvider.Extensions;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Extensions;
using Sitecore.ContentSearch.Linq.Helpers;
using Sitecore.ContentSearch.Linq.Methods;
using Sitecore.ContentSearch.Linq.Nodes;
using Sitecore.ContentSearch.Linq.Parsing;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Linq
{
	public class ElasticSearchQueryMapper : QueryMapper<ElasticSearchQuery>
	{
		private readonly IFieldQueryTranslatorMap<IFieldQueryTranslator> _fieldQueryTranslators;

		public ElasticSearchQueryMapper(ElasticSearchIndexParameters parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			Parameters = parameters;
			ValueFormatter = Parameters.ValueFormatter;
			_fieldQueryTranslators = Parameters.FieldQueryTranslators;
			FieldNameTranslator = Parameters.FieldNameTranslator;
		}

		public ElasticSearchIndexParameters Parameters { get; private set; }
		protected FieldNameTranslator FieldNameTranslator { get; set; }

		public override ElasticSearchQuery MapQuery(IndexQuery query)
		{
			var state = new ElasticSearchQueryMapperState();
			var mappedQuery = Handle(query.RootNode, state);
			return new ElasticSearchQuery(mappedQuery, state.FilterQuery, state.AdditionalQueryMethods, state.VirtualFieldProcessors, state.FacetQueries);
		}

		protected virtual bool ProcessAsVirtualField(FieldNode fieldNode, ConstantNode valueNode, float boost, ComparisonType comparison, ElasticSearchQueryMapperState state, out BaseQuery query)
		{
			query = null;
			if (_fieldQueryTranslators == null)
			{
				return false;
			}
			var translator = _fieldQueryTranslators.GetTranslator(fieldNode.FieldKey.ToLowerInvariant());
			if (translator == null)
			{
				return false;
			}
			
			var formattedValue = ValueFormatter.FormatValueForIndexStorage(valueNode.Value);
			var fieldQuery = translator.TranslateFieldQuery(fieldNode.FieldKey, formattedValue, comparison, FieldNameTranslator); //TODO: does the fieldNode.FieldKey value need to be formatted here?
			if (fieldQuery == null)
			{
				return false;
			}

			var queryList = new List<BaseQuery>();
			if (fieldQuery.FieldComparisons != null)
			{
				foreach (var tuple in fieldQuery.FieldComparisons)
				{
					var indexFieldName = FieldNameTranslator.GetIndexFieldName(tuple.Item1);
					switch (tuple.Item3)
					{
						case ComparisonType.Equal:
							queryList.Add(HandleEqual(indexFieldName, tuple.Item2, boost));
							break;

						case ComparisonType.LessThan:
							queryList.Add(HandleLessThan(indexFieldName, tuple.Item2, boost));
							break;

						case ComparisonType.LessThanOrEqual:
							queryList.Add(HandleLessThanOrEqual(indexFieldName, tuple.Item2, boost));
							break;

						case ComparisonType.GreaterThan:
							queryList.Add(HandleGreaterThan(indexFieldName, tuple.Item2, boost));
							break;

						case ComparisonType.GreaterThanOrEqual:
							queryList.Add(HandleGreaterThanOrEqual(indexFieldName, tuple.Item2, boost));
							break;

						default:
							throw new InvalidOperationException("Unknown comparison type: " + tuple.Item3);
					}
				}
				foreach (var q in queryList)
				{
					if (query == null)
					{
						query = q;
					}
					else
					{
						query &= q;
					}
				}
			}

			if (fieldQuery.QueryMethods != null)
			{
				foreach (var method in fieldQuery.QueryMethods)
				{
					state.AdditionalQueryMethods.Add(method);
				}
			}

			state.VirtualFieldProcessors.Add(translator);

			return true;
		}

		protected virtual void StripAll(AllNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new AllMethod());
		}

		protected virtual void StripAny(AnyNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new AnyMethod());
		}

		protected virtual void StripCast(CastNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new CastMethod(node.TargetType));
		}

		protected virtual void StripCount(CountNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new CountMethod(node.IsLongCount));
		}

		protected virtual void StripElementAt(ElementAtNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new ElementAtMethod(node.Index, node.AllowDefaultValue));
		}

		protected virtual void StripFacetOn(FacetOnNode node, ElasticSearchQueryMapperState state)
		{
			state.FacetQueries.Add(new FacetQuery(node.Field, new[] { node.Field }, node.MinimumNumberOfDocuments, node.FilterValues));
		}

		protected virtual void StripFacetPivotOn(FacetPivotOnNode node, ElasticSearchQueryMapperState state)
		{
			state.FacetQueries.Add(new FacetQuery(null, node.Fields, node.MinimumNumberOfDocuments, node.FilterValues));
		}

		protected virtual void StripFirst(FirstNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new FirstMethod(node.AllowDefaultValue));
		}

		protected virtual void StripGetFacets(GetFacetsNode node, HashSet<QueryMethod> methods)
		{
			methods.Add(new GetFacetsMethod());
		}

		protected virtual void StripGetResults(GetResultsNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new GetResultsMethod(node.Options));
		}

		protected virtual void StripLast(LastNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new LastMethod(node.AllowDefaultValue));
		}

		protected virtual void StripMax(MaxNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new MaxMethod(node.AllowDefaultValue));
		}

		protected virtual void StripMin(MinNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new MinMethod(node.AllowDefaultValue));
		}

		protected virtual void StripOrderBy(OrderByNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			var fieldName = GetFormattedFieldName(node.Field);
			additionalQueryMethods.Add(new OrderByMethod(fieldName, node.FieldType, node.SortDirection));
		}

		protected virtual void StripSelect(SelectNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new SelectMethod(node.Lambda, node.FieldNames));
		}

		protected virtual void StripSingle(SingleNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new SingleMethod(node.AllowDefaultValue));
		}

		protected virtual void StripSkip(SkipNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new SkipMethod(node.Count));
		}

		protected virtual void StripTake(TakeNode node, HashSet<QueryMethod> additionalQueryMethods)
		{
			additionalQueryMethods.Add(new TakeMethod(node.Count));
		}

		protected virtual BaseQuery Handle(QueryNode node, ElasticSearchQueryMapperState state)
		{
			switch (node.NodeType)
			{
				case QueryNodeType.All:
					StripAll((AllNode)node, state.AdditionalQueryMethods);
					return Handle(((AllNode)node).SourceNode, state);

				case QueryNodeType.And:
					return HandleAnd((AndNode)node, state);

				case QueryNodeType.Any:
					StripAny((AnyNode)node, state.AdditionalQueryMethods);
					return Handle(((AnyNode)node).SourceNode, state);

				case QueryNodeType.Between:
					return HandleBetween((BetweenNode)node, state);

				case QueryNodeType.Cast:
					StripCast((CastNode)node, state.AdditionalQueryMethods);
					return Handle(((CastNode)node).SourceNode, state);

				case QueryNodeType.Contains:
					return HandleContains((ContainsNode)node, state);

				case QueryNodeType.Count:
					StripCount((CountNode)node, state.AdditionalQueryMethods);
					return Handle(((CountNode)node).SourceNode, state);

				case QueryNodeType.ElementAt:
					StripElementAt((ElementAtNode)node, state.AdditionalQueryMethods);
					return Handle(((ElementAtNode)node).SourceNode, state);

				case QueryNodeType.EndsWith:
					return HandleEndsWith((EndsWithNode)node, state);

				case QueryNodeType.Equal:
					return HandleEqual((EqualNode)node, state);

				case QueryNodeType.Field:
					return HandleField((FieldNode)node, state);

				case QueryNodeType.First:
					StripFirst((FirstNode)node, state.AdditionalQueryMethods);
					return Handle(((FirstNode)node).SourceNode, state);

				case QueryNodeType.GreaterThan:
					return HandleGreaterThan((GreaterThanNode)node, state);

				case QueryNodeType.GreaterThanOrEqual:
					return HandleGreaterThanOrEqual((GreaterThanOrEqualNode)node, state);

				case QueryNodeType.Last:
					StripLast((LastNode)node, state.AdditionalQueryMethods);
					return Handle(((LastNode)node).SourceNode, state);

				case QueryNodeType.LessThan:
					return HandleLessThan((LessThanNode)node, state);

				case QueryNodeType.LessThanOrEqual:
					return HandleLessThanOrEqual((LessThanOrEqualNode)node, state);

				case QueryNodeType.MatchAll:
					return HandleMatchAll((MatchAllNode)node, state);

				case QueryNodeType.MatchNone:
					return HandleMatchNone((MatchNoneNode)node, state);

				case QueryNodeType.Max:
					StripMax((MaxNode)node, state.AdditionalQueryMethods);
					return Handle(((MaxNode)node).SourceNode, state);

				case QueryNodeType.Min:
					StripMin((MinNode)node, state.AdditionalQueryMethods);
					return Handle(((MinNode)node).SourceNode, state);

				case QueryNodeType.Not:
					return HandleNot((NotNode)node, state);

				case QueryNodeType.Or:
					return HandleOr((OrNode)node, state);

				case QueryNodeType.OrderBy:
					StripOrderBy((OrderByNode)node, state.AdditionalQueryMethods);
					return Handle(((OrderByNode)node).SourceNode, state);

				case QueryNodeType.Select:
					StripSelect((SelectNode)node, state.AdditionalQueryMethods);
					return Handle(((SelectNode)node).SourceNode, state);

				case QueryNodeType.Single:
					StripSingle((SingleNode)node, state.AdditionalQueryMethods);
					return Handle(((SingleNode)node).SourceNode, state);

				case QueryNodeType.Skip:
					StripSkip((SkipNode)node, state.AdditionalQueryMethods);
					return Handle(((SkipNode)node).SourceNode, state);

				case QueryNodeType.StartsWith:
					return HandleStartsWith((StartsWithNode)node, state);

				case QueryNodeType.Take:
					StripTake((TakeNode)node, state.AdditionalQueryMethods);
					return Handle(((TakeNode)node).SourceNode, state);

				case QueryNodeType.Where:
					return HandleWhere((WhereNode)node, state);

				case QueryNodeType.Matches:
					return HandleMatches((MatchesNode)node, state);	

				case QueryNodeType.Filter:
					if (state.FilterQuery != null)
					{
						var filterQuery = state.FilterQuery;
						state.FilterQuery = filterQuery & HandleFilter((FilterNode)node, state);
						break;
					}
					state.FilterQuery = HandleFilter((FilterNode)node, state);
					break;

				case QueryNodeType.GetResults:
					StripGetResults((GetResultsNode)node, state.AdditionalQueryMethods);
					return Handle(((GetResultsNode)node).SourceNode, state);

				case QueryNodeType.GetFacets:
					StripGetFacets((GetFacetsNode)node, state.AdditionalQueryMethods);
					return Handle(((GetFacetsNode)node).SourceNode, state);

				case QueryNodeType.FacetOn:
					StripFacetOn((FacetOnNode)node, state);
					return Handle(((FacetOnNode)node).SourceNode, state);

				case QueryNodeType.FacetPivotOn:
					StripFacetPivotOn((FacetPivotOnNode)node, state);
					return Handle(((FacetPivotOnNode)node).SourceNode, state);

				case QueryNodeType.WildcardMatch:
					return HandleWildcardMatch((WildcardMatchNode)node, state);

				case QueryNodeType.Like:
					return HandleLike((LikeNode)node, state);

				default:
					throw new NotSupportedException(string.Format("Unknown query node type: '{0}'", node.NodeType));
			}

			return Handle(((FilterNode)node).SourceNode, state);
		}

		protected virtual BaseQuery HandleAnd(AndNode node, ElasticSearchQueryMapperState state)
		{
			var query1 = Handle(node.LeftNode, state);
			var query2 = Handle(node.RightNode, state);
			
			if (!query1)
			{
				return query1;
			}
			return query1 & query2;
		}

		protected virtual BaseQuery HandleNot(NotNode node, ElasticSearchQueryMapperState state)
		{
			var query = Handle(node.Operand, state);

			//TODO: this works for basic term queries. While the LuceneProvider is somewhat the same, the SolrProvider had extra logic presumably to handle specific cases.
			return !query; 
		}

		protected virtual BaseQuery HandleOr(OrNode node, ElasticSearchQueryMapperState state)
		{
			//TODO: the code below specifically handles IsNullOrEmpty method, seems like a lot to handle one case... is there a better place for it?
			//Lucene provider doesn't have this code, but Solr provider does
			if (node.LeftNode.NodeType == QueryNodeType.Equal && node.RightNode.NodeType == QueryNodeType.Equal)
			{
				var leftNode = (EqualNode)node.LeftNode;
				var rightNode = (EqualNode)node.RightNode;
				if (leftNode.RightNode.NodeType == QueryNodeType.Constant && rightNode.RightNode.NodeType == QueryNodeType.Constant)
				{
					var leftNodeValue = ((ConstantNode)leftNode.RightNode).Value;
					var rightNodeValue = ((ConstantNode)rightNode.RightNode).Value;
					if ((string)leftNodeValue == string.Empty && rightNodeValue == null)
					{
						var fieldName = ((FieldNode)leftNode.LeftNode).FieldKey;
						//TODO: this query works for 99% of items, however if a field contains a stopword and only a stopword, then it's treated as "missing" by ES
						//For example, say you have an item whose "Title" field contains just the word "To" (which is a stopword), if you try to search for all items 
						//without a value in the "Title" field (i.e. the field is missing), ES will still return the item whose "Title" field contains just the word "To".
						//There's likely a better query to use... or maybe not, maybe it can only be done with analyzers. who could say?
						return Query.Filtered(fq => fq.Filter(f => f.Missing(fieldName)));
					}
				}
			}

			var query1 = Handle(node.LeftNode, state);
			var query2 = Handle(node.RightNode, state);
			if (query1)
			{
				return query1;
			}

			return query1 | query2;
		}

		protected virtual BaseQuery HandleBetween(BetweenNode node, ElasticSearchQueryMapperState state)
		{
			var excludeLowerBound = !(node.Inclusion == Inclusion.Both || node.Inclusion == Inclusion.Lower);
			var excludeUpperBound = !(node.Inclusion == Inclusion.Both || node.Inclusion == Inclusion.Upper);
			var fieldName = GetFormattedFieldName(node.Field);
			var lowerBound = ValueFormatter.FormatValueForIndexStorage(node.From);
			var upperBound = ValueFormatter.FormatValueForIndexStorage(node.To);

			return Query.Range(delegate(RangeQueryDescriptor<dynamic> descriptor)
			{
				descriptor.From(lowerBound.ToString());
				if (excludeLowerBound)
					descriptor.FromExclusive();

				descriptor.To(upperBound.ToString());
				if (excludeUpperBound)
					descriptor.ToExclusive();

				descriptor.OnField(fieldName);
			});
		}

		protected virtual BaseQuery HandleContains(ContainsNode node, ElasticSearchQueryMapperState state)
		{
			var fieldName = GetFormattedFieldName(node);
			var valueNode = QueryHelper.GetValueNode<string>(node);

			//wildcard query values should be lowercase, even if the value is stored case-sensitively
			//not sure why it needs to be this way - and there's probably some way to make it work either way using analyzers...
			var queryValue = ValueFormatter.FormatValueForIndexStorage(valueNode.Value).ToString().ToLowerInvariant();
			
			//TODO: evidently, wildcard queries do not scale well for large indexes
			//therefore, this query should be replaced with something else. 
			//rumor has it the use of n-gram analyzers and regular term queries works better than wildcard queries.
			//question is, how to do this in generic terms so that it works "out of the box"?
			//probably an easy way, just not today...
			return Query.Wildcard(fieldName, "*" + queryValue + "*"); 
		}

		protected virtual BaseQuery HandleEndsWith(EndsWithNode node, ElasticSearchQueryMapperState state)
		{
			var fieldName = GetFormattedFieldName(node);
			var valueNode = QueryHelper.GetValueNode<string>(node);
			
			var queryValue = ValueFormatter.FormatValueForIndexStorage(valueNode.Value);

			//TODO: same as the HandleContains method... is there a better way to do this without wildcard queries?
			return Query.Wildcard(fieldName, "*" + queryValue);
		}

		protected virtual BaseQuery HandleStartsWith(StartsWithNode node, ElasticSearchQueryMapperState state)
		{
			var fieldName = GetFormattedFieldName(node);
			var valueNode = QueryHelper.GetValueNode<string>(node);
			
			var queryValue = ValueFormatter.FormatValueForIndexStorage(valueNode.Value);

			return Query.Prefix(fieldName, queryValue.ToString());
		}

		protected virtual BaseQuery HandleEqual(EqualNode node, ElasticSearchQueryMapperState state)
		{
			BaseQuery query;
			
			var fieldNode = node.GetFieldNode();
			var valueNode = QueryHelper.GetValueNode<object>(node);
			
			if (ProcessAsVirtualField(fieldNode, valueNode, node.Boost, ComparisonType.Equal, state, out query))
			{
				return query;
			}
			
			return HandleEqual(fieldNode.FieldKey, valueNode.Value, node.Boost);
		}

		protected virtual BaseQuery HandleEqual(string fieldName, object fieldValue, float boost)
		{
			fieldName = GetFormattedFieldName(fieldName);
			var formattedValue = ValueFormatter.FormatValueForIndexStorage(fieldValue).ToStringOrEmpty();

			var query = Query.Term(fieldName, formattedValue);

			if (Math.Abs(boost - 1) > float.Epsilon)
			{
				query = query.Boost(boost);
			}
			return query;
		}

		protected virtual BaseQuery HandleField(FieldNode node, ElasticSearchQueryMapperState state)
		{
			if (node.FieldType != typeof(bool))
			{
				throw new NotSupportedException(string.Format("The query node type '{0}' is not supported in this context.", node.NodeType));
			}
			
			var fieldName = GetFormattedFieldName(node.FieldKey);
			return Query.Term(fieldName, true.ToString());
		}

		protected virtual BaseQuery HandleFilter(FilterNode node, ElasticSearchQueryMapperState state)
		{
			return Handle(node.PredicateNode, state);
		}

		protected virtual BaseQuery HandleGreaterThan(GreaterThanNode node, ElasticSearchQueryMapperState state)
		{
			BaseQuery query;

			var fieldNode = node.GetFieldNode();
			var valueNode = QueryHelper.GetValueNode(node, fieldNode.FieldType);
			
			if (ProcessAsVirtualField(fieldNode, valueNode, node.Boost, ComparisonType.GreaterThan, state, out query))
			{
				return query;
			}
			
			return HandleGreaterThan(fieldNode.FieldKey, valueNode.Value, node.Boost);
		}

		protected virtual BaseQuery HandleGreaterThan(string fieldName, object value, float boost)
		{
			fieldName = GetFormattedFieldName(fieldName);
			var formattedValue = ValueFormatter.FormatValueForIndexStorage(value);
			
			var query = Query.Range(descriptor => descriptor.From(formattedValue.ToString()).FromExclusive().OnField(fieldName));

			if (Math.Abs(boost - 1f) > float.Epsilon)
			{
				query = query.Boost(boost);
			}
			return query;
		}

		protected virtual BaseQuery HandleGreaterThanOrEqual(GreaterThanOrEqualNode node, ElasticSearchQueryMapperState state)
		{
			BaseQuery query;

			var fieldNode = node.GetFieldNode();
			var valueNode = QueryHelper.GetValueNode(node, fieldNode.FieldType);

			if (ProcessAsVirtualField(fieldNode, valueNode, node.Boost, ComparisonType.GreaterThanOrEqual, state, out query))
			{
				return query;
			}
			
			return HandleGreaterThanOrEqual(fieldNode.FieldKey, valueNode.Value, node.Boost);
		}

		protected virtual BaseQuery HandleGreaterThanOrEqual(string fieldName, object value, float boost)
		{
			fieldName = GetFormattedFieldName(fieldName);
			var formattedValue = ValueFormatter.FormatValueForIndexStorage(value);
			
			var query = Query.Range(descriptor => descriptor.From(formattedValue.ToString()).OnField(fieldName));
			if (Math.Abs(boost - 1f) > float.Epsilon)
			{
				query = query.Boost(boost);
			}
			return query;
		}

		protected virtual BaseQuery HandleLessThan(LessThanNode node, ElasticSearchQueryMapperState state)
		{
			BaseQuery query;
			
			var fieldNode = node.GetFieldNode();
			var valueNode = QueryHelper.GetValueNode(node, fieldNode.FieldType);

			if (ProcessAsVirtualField(fieldNode, valueNode, node.Boost, ComparisonType.LessThan, state, out query))
			{
				return query;
			}
			
			return HandleLessThan(fieldNode.FieldKey, valueNode.Value, node.Boost);
		}

		protected virtual BaseQuery HandleLessThan(string fieldName, object value, float boost)
		{
			fieldName = GetFormattedFieldName(fieldName);
			var formattedValue = ValueFormatter.FormatValueForIndexStorage(value);
			
			var query = Query.Range(descriptor => descriptor.To(formattedValue.ToString()).ToExclusive().OnField(fieldName));
			if (Math.Abs(boost - 1f) > float.Epsilon)
			{
				query = query.Boost(boost);
			}
			return query;
		}

		protected virtual BaseQuery HandleLessThanOrEqual(LessThanOrEqualNode node, ElasticSearchQueryMapperState state)
		{
			BaseQuery query;
			
			var fieldNode = node.GetFieldNode();
			var valueNode = QueryHelper.GetValueNode(node, fieldNode.FieldType);

			if (ProcessAsVirtualField(fieldNode, valueNode, node.Boost, ComparisonType.LessThanOrEqual, state, out query))
			{
				return query;
			}
			
			return HandleLessThanOrEqual(fieldNode.FieldKey, valueNode.Value, node.Boost);
		}

		protected virtual BaseQuery HandleLessThanOrEqual(string fieldName, object value, float boost)
		{
			fieldName = GetFormattedFieldName(fieldName);
			var formattedValue = ValueFormatter.FormatValueForIndexStorage(value);
			
			var query = Query.Range(descriptor => descriptor.To(formattedValue.ToString()).OnField(fieldName));
			if (Math.Abs(boost - 1) > float.Epsilon)
			{
				query = query.Boost(boost);
			}
			return query;
		}

		protected BaseQuery HandleLike(LikeNode node, ElasticSearchQueryMapperState mappingState)
		{
			var fieldName = GetFormattedFieldName(node);
			var valueNode = QueryHelper.GetValueNode<string>(node);
			var formattedValue = ValueFormatter.FormatValueForIndexStorage(valueNode.Value);

			var query =
				Query.Fuzzy(
					descriptor =>
					descriptor.OnField(fieldName)
							  .MinSimilarity(node.MinimumSimilarity)
							  .Like(formattedValue.ToStringOrEmpty())
							  .Boost(node.Boost));
			
			return query;
		}

		protected virtual BaseQuery HandleMatchAll(MatchAllNode node, ElasticSearchQueryMapperState state)
		{
			return Query.MatchAll();
		}

		/// <summary>
		/// Regex query not currently supported in NEST, and there's no straightforward way to append raw query text to any parsed queries.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		protected virtual BaseQuery HandleMatches(MatchesNode node, ElasticSearchQueryMapperState state)
		{
			//var fieldName = GetFormattedFieldName(node);
			//var valueNode = QueryHelper.GetValueNode<string>(node);
			//var formattedValue = ValueFormatter.FormatValueForIndexStorage(valueNode.Value);
			
			//using (var sw = new StringWriter())
			//{ 
			//	using (var jw = new JsonTextWriter(sw))
			//	{
			//		jw.WriteStartObject();
			//			jw.WritePropertyName("regexp");
			//			jw.WriteStartObject();
			//				jw.WritePropertyName(fieldName);
			//				jw.WriteStartObject();
			//					jw.WritePropertyName("value");
			//					jw.WriteValue(formattedValue);
			//					if (Math.Abs(node.Boost - 1f) > float.Epsilon)
			//					{
			//						jw.WritePropertyName("boost");
			//						jw.WriteValue(node.Boost);
			//					}
			//				jw.WriteEndObject();
			//				if (node.RegexOptions != null && !string.IsNullOrEmpty(node.RegexOptions.ToString()))
			//				{
			//					jw.WritePropertyName("flags");
			//					jw.WriteValue(node.RegexOptions);	
			//				}
			//			jw.WriteEndObject();
			//		jw.WriteEndObject();
			//	}

			//	var rawQuery = sw.ToString();
			//	return rawQuery;
			//}

			//rawQuery = "{\"regexp\" : { \"" + fieldName + "\" : { \"value\" : \"" + formattedValue + "\", \"flags\" : \"" + node.RegexOptions + "\" } }";

			throw new NotImplementedException("Matches expression is not implemented (i.e. no regex queries)");
		}

		protected virtual BaseQuery HandleMatchNone(MatchNoneNode node, ElasticSearchQueryMapperState state)
		{
			//TODO: does this actually work? not sure about the syntax...
			return !Query.MatchAll();
			//return Query.Bool(descriptor => descriptor.MustNot(new[] { Query.MatchAll() }));
		}

		protected virtual BaseQuery HandleWhere(WhereNode node, ElasticSearchQueryMapperState state)
		{
			var query1 = Handle(node.PredicateNode, state);
			var query2 = Handle(node.SourceNode, state);
			if (query1 == Query.MatchAll() && query2 == Query.MatchAll())
			{
				return query1;
			}
			if (query1 == Query.MatchAll() || query2 == Query.MatchAll())
			{
				if (query1 != Query.MatchAll())
				{
					return query1;
				}
				if (query2 != Query.MatchAll())
				{
					return query2;
				}
			}
			
			return query1 & query2;
		}

		protected BaseQuery HandleWildcardMatch(WildcardMatchNode node, ElasticSearchQueryMapperState mappingState)
		{
			var fieldName = GetFormattedFieldName(node);
			var valueNode = QueryHelper.GetValueNode<string>(node);

			var queryValue = ValueFormatter.FormatValueForIndexStorage(valueNode.Value);

			//TODO: same as the HandleContains method... is there a better way to do this without wildcard queries?
			return Query.Wildcard(fieldName, queryValue.ToString());
		}

		protected string GetFormattedFieldName(BinaryNode node)
		{
			var fieldNode = node.GetFieldNode();
			var fieldName = GetFormattedFieldName(fieldNode.FieldKey);
			return fieldName;
		}

		protected string GetFormattedFieldName(string rawFieldName)
		{
			return rawFieldName.ToLowerInvariant().Replace(" ", "_");
		}
	}
}

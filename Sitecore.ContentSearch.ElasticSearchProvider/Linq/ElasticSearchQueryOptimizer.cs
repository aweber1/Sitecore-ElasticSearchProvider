using System.Linq;
using Sitecore.ContentSearch.Linq.Extensions;
using Sitecore.ContentSearch.Linq.Nodes;
using Sitecore.ContentSearch.Linq.Parsing;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Linq
{
	public class ElasticSearchQueryOptimizer : QueryOptimizer<ElasticSearchQueryOptimizerState>
	{
		private bool? GetBoolValue(QueryNode node)
		{
			if (node.NodeType == QueryNodeType.MatchAll)
			{
				return true;
			}
			if (node.NodeType == QueryNodeType.MatchNone)
			{
				return false;
			}
			if (node.NodeType == QueryNodeType.Constant)
			{
				var node2 = (ConstantNode)node;
				if (node2.Type == typeof(bool))
				{
					return (bool)node2.Value;
				}
			}
			return null;
		}

		protected override QueryNode Visit(QueryNode node, ElasticSearchQueryOptimizerState state)
		{
			switch (node.NodeType)
			{
				case QueryNodeType.All:
					return VisitAll((AllNode)node, state);

				case QueryNodeType.And:
					return VisitAnd((AndNode)node, state);

				case QueryNodeType.Any:
					return VisitAny((AnyNode)node, state);

				case QueryNodeType.Between:
				case QueryNodeType.Custom:
				case QueryNodeType.EndsWith:
				case QueryNodeType.Field:
				case QueryNodeType.MatchAll:
				case QueryNodeType.MatchNone:
				case QueryNodeType.StartsWith:
				case QueryNodeType.Matches:
					return node;

				case QueryNodeType.Boost:
					return VisitBoost((BoostNode)node, state);

				case QueryNodeType.Cast:
					return VisitCast((CastNode)node, state);

				case QueryNodeType.Constant:
					return VisitConstant((ConstantNode)node, state);

				case QueryNodeType.Contains:
					return VisitContains((ContainsNode)node, state);

				case QueryNodeType.Count:
					return VisitCount((CountNode)node, state);

				case QueryNodeType.ElementAt:
					return VisitElementAt((ElementAtNode)node, state);

				case QueryNodeType.Equal:
					return VisitEqual((EqualNode)node, state);

				case QueryNodeType.First:
					return VisitFirst((FirstNode)node, state);

				case QueryNodeType.GreaterThan:
					return VisitGreaterThan((GreaterThanNode)node, state);

				case QueryNodeType.GreaterThanOrEqual:
					return VisitGreaterThanOrEqual((GreaterThanOrEqualNode)node, state);

				case QueryNodeType.Last:
					return VisitLast((LastNode)node, state);

				case QueryNodeType.LessThan:
					return VisitLessThan((LessThanNode)node, state);

				case QueryNodeType.LessThanOrEqual:
					return VisitLessThanOrEqual((LessThanOrEqualNode)node, state);

				case QueryNodeType.Max:
					return VisitMax((MaxNode)node, state);

				case QueryNodeType.Min:
					return VisitMin((MinNode)node, state);

				case QueryNodeType.Not:
					return VisitNot((NotNode)node, state);

				case QueryNodeType.NotEqual:
					return VisitNotEqual((NotEqualNode)node, state);

				case QueryNodeType.Or:
					return VisitOr((OrNode)node, state);

				case QueryNodeType.OrderBy:
					return VisitOrderBy((OrderByNode)node, state);

				case QueryNodeType.Select:
					return VisitSelect((SelectNode)node, state);

				case QueryNodeType.Single:
					return VisitSingle((SingleNode)node, state);

				case QueryNodeType.Skip:
					return VisitSkip((SkipNode)node, state);

				case QueryNodeType.Take:
					return VisitTake((TakeNode)node, state);

				case QueryNodeType.Where:
					return VisitWhere((WhereNode)node, state);

				case QueryNodeType.Filter:
					return VisitFilter((FilterNode)node, state);

				case QueryNodeType.GetResults:
					return VisitGetResults((GetResultsNode)node, state);

				case QueryNodeType.Negate:
					return VisitNegate((NegateNode)node, state);

				case QueryNodeType.GetFacets:
					return VisitGetFacets((GetFacetsNode)node, state);

				case QueryNodeType.FacetOn:
					return VisitFacetOn((FacetOnNode)node, state);

				case QueryNodeType.FacetPivotOn:
					return VisitFacetPivotOn((FacetPivotOnNode)node, state);

				case QueryNodeType.WildcardMatch:
					return VisitWildcardMatch((WildcardMatchNode)node, state);

				case QueryNodeType.Like:
					return VisitLike((LikeNode)node, state);
			}
			return node;

		}

		protected virtual QueryNode VisitAll(AllNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.SourceNode, state);
			var node3 = Visit(node.PredicateNode, state);
			if (node3.NodeType == QueryNodeType.MatchAll)
			{
				return new AllNode(node2, node3);
			}
			return new AllNode(VisitAnd(new AndNode(node2, node3), state), new MatchAllNode());
		}

		protected virtual QueryNode VisitAnd(AndNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.LeftNode, state);
			var node3 = Visit(node.RightNode, state);
			var boolValue = GetBoolValue(node2);
			var nullable2 = GetBoolValue(node3);
			if (!boolValue.HasValue && !nullable2.HasValue)
			{
				return new AndNode(node2, node3);
			}
			if (boolValue.HasValue && nullable2.HasValue)
			{
				if (boolValue.Value && nullable2.Value)
				{
					return new MatchAllNode();
				}
				return new MatchNoneNode();
			}
			if (boolValue.HasValue)
			{
				return !boolValue.Value ? new MatchNoneNode() : node3;
			}
			return !nullable2.Value ? new MatchNoneNode() : node2;
		}

		protected virtual QueryNode VisitAny(AnyNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.SourceNode, state);
			var node3 = Visit(node.PredicateNode, state);
			if (node3.NodeType == QueryNodeType.MatchAll)
			{
				return new AnyNode(node2, node3);
			}
			return new AnyNode(VisitAnd(new AndNode(node2, node3), state), new MatchAllNode());
		}

		protected virtual QueryNode VisitBoost(BoostNode node, ElasticSearchQueryOptimizerState state)
		{
			state.Boost = node.Boost;
			return Visit(node.Operand, state);
		}

		protected virtual QueryNode VisitCast(CastNode node, ElasticSearchQueryOptimizerState state)
		{
			return new CastNode(Visit(node.SourceNode, state), node.TargetType);
		}

		protected virtual QueryNode VisitConstant(ConstantNode node, ElasticSearchQueryOptimizerState state)
		{
			var type = typeof(IQueryable);
			if (node.Type.IsAssignableTo(type))
			{
				return new MatchAllNode();
			}
			return node;
		}

		protected virtual QueryNode VisitContains(ContainsNode node, ElasticSearchQueryOptimizerState state)
		{
			state.Boost = 1f;
			var node2 = Visit(node.LeftNode, state);
			return new ContainsNode(node2, Visit(node.RightNode, state), state.Boost);
		}

		protected virtual QueryNode VisitCount(CountNode node, ElasticSearchQueryOptimizerState state)
		{
			QueryNode node2 = Visit(node.SourceNode, state);
			QueryNode node3 = Visit(node.PredicateNode, state);
			if (node3.NodeType == QueryNodeType.MatchAll)
			{
				return new CountNode(node2, node3, node.IsLongCount);
			}
			return new CountNode(VisitAnd(new AndNode(node2, node3), state), new MatchAllNode(), node.IsLongCount);
		}

		protected virtual QueryNode VisitElementAt(ElementAtNode node, ElasticSearchQueryOptimizerState state)
		{
			return new ElementAtNode(Visit(node.SourceNode, state), node.Index, node.AllowDefaultValue);
		}

		protected virtual QueryNode VisitEqual(EqualNode node, ElasticSearchQueryOptimizerState state)
		{
			state.Boost = 1f;
			var node2 = Visit(node.LeftNode, state);
			return new EqualNode(node2, Visit(node.RightNode, state), state.Boost);
		}

		protected virtual QueryNode VisitFacetOn(FacetOnNode node, ElasticSearchQueryOptimizerState state)
		{
			return new FacetOnNode(Visit(node.SourceNode, state), node.Field, node.MinimumNumberOfDocuments, node.FilterValues);
		}

		protected virtual QueryNode VisitFacetPivotOn(FacetPivotOnNode node, ElasticSearchQueryOptimizerState state)
		{
			return new FacetPivotOnNode(Visit(node.SourceNode, state), node.Fields, node.MinimumNumberOfDocuments, node.FilterValues);
		}

		protected virtual QueryNode VisitFilter(FilterNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.SourceNode, state);
			return new FilterNode(node2, Visit(node.PredicateNode, state));
		}

		protected virtual QueryNode VisitFirst(FirstNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.SourceNode, state);
			var node3 = Visit(node.PredicateNode, state);
			if (node3.NodeType == QueryNodeType.MatchAll)
			{
				return new FirstNode(node2, node3, node.AllowDefaultValue);
			}
			return new FirstNode(VisitAnd(new AndNode(node2, node3), state), new MatchAllNode(), node.AllowDefaultValue);
		}

		protected virtual QueryNode VisitGetFacets(GetFacetsNode node, ElasticSearchQueryOptimizerState state)
		{
			return new GetFacetsNode(Visit(node.SourceNode, state));
		}

		protected virtual QueryNode VisitGetResults(GetResultsNode node, ElasticSearchQueryOptimizerState state)
		{
			return new GetResultsNode(Visit(node.SourceNode, state), node.Options);
		}

		protected virtual QueryNode VisitGreaterThan(GreaterThanNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.LeftNode, state);
			return new GreaterThanNode(node2, Visit(node.RightNode, state));
		}

		protected virtual QueryNode VisitGreaterThanOrEqual(GreaterThanOrEqualNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.LeftNode, state);
			return new GreaterThanOrEqualNode(node2, Visit(node.RightNode, state));
		}

		protected virtual QueryNode VisitLast(LastNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.SourceNode, state);
			var node3 = Visit(node.PredicateNode, state);
			if (node3.NodeType == QueryNodeType.MatchAll)
			{
				return new LastNode(node2, node3, node.AllowDefaultValue);
			}
			return new LastNode(VisitAnd(new AndNode(node2, node3), state), new MatchAllNode(), node.AllowDefaultValue);
		}

		protected virtual QueryNode VisitLessThan(LessThanNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.LeftNode, state);
			return new LessThanNode(node2, Visit(node.RightNode, state));
		}

		protected virtual QueryNode VisitLessThanOrEqual(LessThanOrEqualNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.LeftNode, state);
			return new LessThanOrEqualNode(node2, Visit(node.RightNode, state));
		}

		protected virtual QueryNode VisitLike(LikeNode node, ElasticSearchQueryOptimizerState state)
		{
			state.Boost = 1f;
			var node2 = Visit(node.LeftNode, state);
			return new LikeNode(node2, Visit(node.RightNode, state), node.MinimumSimilarity, state.Boost);
		}

		protected virtual QueryNode VisitMax(MaxNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.SourceNode, state);
			var node3 = Visit(node.PredicateNode, state);
			if (node3.NodeType == QueryNodeType.MatchAll)
			{
				return new MaxNode(node2, node3, node.AllowDefaultValue);
			}
			return new MaxNode(VisitAnd(new AndNode(node2, node3), state), new MatchAllNode(), node.AllowDefaultValue);
		}

		protected virtual QueryNode VisitMin(MinNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.SourceNode, state);
			var node3 = Visit(node.PredicateNode, state);
			if (node3.NodeType == QueryNodeType.MatchAll)
			{
				return new MinNode(node2, node3, node.AllowDefaultValue);
			}
			return new MinNode(VisitAnd(new AndNode(node2, node3), state), new MatchAllNode(), node.AllowDefaultValue);
		}

		protected virtual QueryNode VisitNegate(NegateNode node, ElasticSearchQueryOptimizerState state)
		{
			return new NegateNode(Visit(node.Operand, state));
		}

		protected virtual QueryNode VisitNot(NotNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.Operand, state);
			if (node2.NodeType == QueryNodeType.Not)
			{
				return ((NotNode)node2).Operand;
			}
			var boolValue = GetBoolValue(node2);
			if (!boolValue.HasValue)
			{
				return new NotNode(node2);
			}
			if (boolValue.Value)
			{
				return new MatchNoneNode();
			}
			return new MatchAllNode();
		}

		protected virtual QueryNode VisitNotEqual(NotEqualNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = new NotNode(new EqualNode(node.LeftNode, node.RightNode, node.Boost));
			return Visit(node2, state);
		}

		protected virtual QueryNode VisitOr(OrNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.LeftNode, state);
			var node3 = Visit(node.RightNode, state);
			var boolValue = GetBoolValue(node2);
			var nullable2 = GetBoolValue(node3);
			if (!boolValue.HasValue && !nullable2.HasValue)
			{
				return new OrNode(node2, node3);
			}
			if (boolValue.HasValue && nullable2.HasValue)
			{
				if (!boolValue.Value && !nullable2.Value)
				{
					return new MatchNoneNode();
				}
				return new MatchAllNode();
			}
			return boolValue.HasValue ? node3 : node2;
		}

		protected virtual QueryNode VisitOrderBy(OrderByNode node, ElasticSearchQueryOptimizerState state)
		{
			return new OrderByNode(Visit(node.SourceNode, state), node.Field, node.FieldType, node.SortDirection);
		}

		protected virtual QueryNode VisitSelect(SelectNode node, ElasticSearchQueryOptimizerState state)
		{
			return new SelectNode(Visit(node.SourceNode, state), node.Lambda, node.FieldNames);
		}

		protected virtual QueryNode VisitSingle(SingleNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.SourceNode, state);
			var node3 = Visit(node.PredicateNode, state);
			if (node3.NodeType == QueryNodeType.MatchAll)
			{
				return new SingleNode(node2, node3, node.AllowDefaultValue);
			}
			return new SingleNode(VisitAnd(new AndNode(node2, node3), state), new MatchAllNode(), node.AllowDefaultValue);
		}

		protected virtual QueryNode VisitSkip(SkipNode node, ElasticSearchQueryOptimizerState state)
		{
			return new SkipNode(Visit(node.SourceNode, state), node.Count);
		}

		protected virtual QueryNode VisitTake(TakeNode node, ElasticSearchQueryOptimizerState state)
		{
			return new TakeNode(Visit(node.SourceNode, state), node.Count);
		}

		protected virtual QueryNode VisitWhere(WhereNode node, ElasticSearchQueryOptimizerState state)
		{
			var node2 = Visit(node.SourceNode, state);
			var node3 = Visit(node.PredicateNode, state);
			var boolValue = GetBoolValue(node3);
			if (!boolValue.HasValue)
			{
				if (node2 is MatchAllNode)
				{
					return node3;
				}
				if (node2 is MatchNoneNode)
				{
					return node2;
				}
				return new WhereNode(node3, node2);
			}
			return boolValue.Value ? node2 : new MatchNoneNode();
		}

		protected virtual QueryNode VisitWildcardMatch(WildcardMatchNode node, ElasticSearchQueryOptimizerState state)
		{
			state.Boost = 1f;
			var node2 = Visit(node.LeftNode, state);
			return new WildcardMatchNode(node2, Visit(node.RightNode, state), state.Boost);
		}
	}
}

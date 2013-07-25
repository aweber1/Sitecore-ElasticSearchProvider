using Sitecore.ContentSearch.Linq.Helpers;
using Sitecore.ContentSearch.Linq.Nodes;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Extensions
{
	public static class BinaryNodeExtensions
	{
		public static FieldNode GetFieldNode(this BinaryNode node)
		{
			return QueryHelper.GetFieldNode(node);
		}
	}
}

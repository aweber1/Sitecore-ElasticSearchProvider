using Sitecore.ContentSearch.Linq.Parsing;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Linq
{
	public class ElasticSearchQueryOptimizerState : QueryOptimizerState
	{
		public ElasticSearchQueryOptimizerState()
		{
			Boost = 1f;
		}

		public float Boost { get; set; }
	}
}

using Sitecore.ContentSearch.Diagnostics;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Crawlers
{
	public class ElasticSearchDatabaseCrawler : AbstractProviderCrawler
	{
		public override void Initialize(ISearchIndex searchIndex)
		{
			base.Initialize(searchIndex);
			if ((Operations != null) || !ElasticSearchContentSearchManager.IsEnabled) 
				return;

			var esIndex = searchIndex as ElasticSearchIndex;
			if (esIndex == null)
				return;

			Operations = new ElasticSearchIndexOperations(esIndex);
			var msg = string.Format("[Index={0}] Initializing ElasticSearchDatabaseCrawler. DB:{1} / Root:{2}", searchIndex.Name, Database, Root);
			CrawlingLog.Log.Info(msg);
		}
	}
}

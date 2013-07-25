using Sitecore.ContentSearch.Utilities;
using Sitecore.Diagnostics;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchConfiguration : ProviderIndexSearchConfiguration
	{
		public virtual void AddIndex(ISearchIndex index)
		{
			Assert.ArgumentNotNull(index, "index");
			Indexes[index.Name] = index;

			if (DefaultIndexConfiguration == null) 
				return;

			if (index.Configuration == null)
			{
				index.Configuration = DefaultIndexConfiguration;
			}
			else
			{
				var defaultIndexConfiguration = DefaultIndexConfiguration as ElasticSearchIndexConfiguration;
				var configuration = index.Configuration as ElasticSearchIndexConfiguration;
				if (defaultIndexConfiguration != null && configuration != null)
				{
					configuration.MergeConfiguration(defaultIndexConfiguration);
				}
			}

			index.Initialize();
		}
	}
}

using System;
using System.Collections.Generic;
using Sitecore.Configuration;
using Sitecore.Diagnostics;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public static class ElasticSearchContentSearchManager
	{
		// Fields
		private static string _serviceAddress;

		// Methods
		public static ISearchIndex GetIndex(string id)
		{
			Assert.IsNotNullOrEmpty(id, "id");
			return !SearchConfiguration.Indexes.ContainsKey(id) ? null : SearchConfiguration.Indexes[id];
		}

		public static void Initialize()
		{
			foreach (var index in Indexes)
			{
				index.Initialize();
			}
		}

		public static int IndexCount
		{
			get
			{
				return SearchConfiguration.Indexes.Count;
			}
		}

		public static IEnumerable<ISearchIndex> Indexes
		{
			get
			{
				return SearchConfiguration.Indexes.Values;
			}
		}

		public static bool IsEnabled
		{
			get
			{
				return ((SupportedProviders)Enum.Parse(typeof(SupportedProviders), Settings.GetSetting("ContentSearch.Provider", "ElasticSearch"))) == SupportedProviders.ElasticSearch;
			}
		}

		private static ElasticSearchConfiguration SearchConfiguration
		{
			get
			{
				return (ContentSearchManager.SearchConfiguration as ElasticSearchConfiguration);
			}
		}

		public static string ServiceAddress
		{
			get
			{
				return (_serviceAddress ?? (_serviceAddress = Settings.GetSetting("ContentSearch.ElasticSearch.ServiceBaseAddress", "http://localhost:9200/")));
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Security;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchContext : IProviderSearchContext
	{
		public ISearchIndex Index { get; private set; }
		public SearchSecurityOptions SecurityOptions { get; private set; }

		public ElasticSearchContext(ElasticSearchIndex index,
									SearchSecurityOptions securityOptions = SearchSecurityOptions.EnableSecurityCheck)
		{
			Index = index;
			SecurityOptions = securityOptions;
		}

		public IQueryable<TItem> GetQueryable<TItem>() where TItem : new()
		{
			return GetQueryable<TItem>(null);
		}

		public IQueryable<TItem> GetQueryable<TItem>(IExecutionContext executionContext) where TItem : new()
		{
			var index = new LinqToElasticSearchIndex<TItem>(this, executionContext);
			//index.TraceWriter = new LoggingTraceWriter(SearchLog.Log);
			return index.GetQueryable();
		}

		public IEnumerable<SearchIndexTerm> GetTermsByFieldName(string fieldName, string prefix)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			
		}
	}
}

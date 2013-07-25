using System;
using System.Collections.Generic;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Indexing;
using Sitecore.ContentSearch.Linq.Parsing;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Linq
{
	public class ElasticSearchIndex<TItem> : Index<TItem, ElasticSearchQuery>
	{
		// Fields
		private readonly ElasticSearchIndexParameters _parameters;
		private readonly QueryMapper<ElasticSearchQuery> _queryMapper;
		private readonly ElasticSearchQueryOptimizer _queryOptimizer;
		
		public ElasticSearchIndex(ElasticSearchIndexParameters parameters)
		{
			_queryOptimizer = new ElasticSearchQueryOptimizer();
			if (parameters == null)
			{
				throw new ArgumentNullException("parameters");
			}
			_queryMapper = new ElasticSearchQueryMapper(parameters);
			_parameters = parameters;
		}

		public override TResult Execute<TResult>(ElasticSearchQuery query)
		{
			return default(TResult);
		}

		public override IEnumerable<TElement> FindElements<TElement>(ElasticSearchQuery query)
		{
			return new List<TElement>();
		}

		protected override QueryMapper<ElasticSearchQuery> QueryMapper
		{
			get { return _queryMapper; }
		}

		public ElasticSearchIndexParameters Parameters
		{
			get { return _parameters; }
		}

		protected override IQueryOptimizer QueryOptimizer
		{
			get { return _queryOptimizer; }
		}

		protected override FieldNameTranslator FieldNameTranslator
		{
			get { return _parameters.FieldNameTranslator; }
		}
	}
}

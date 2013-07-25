using System.Collections.Generic;
using Nest;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Methods;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Linq
{
	public class ElasticSearchQueryMapperState
	{
		public ElasticSearchQueryMapperState()
		{
			AdditionalQueryMethods = new HashSet<QueryMethod>();
			VirtualFieldProcessors = new List<IFieldQueryTranslator>();
			FacetQueries = new List<FacetQuery>();
		}

		// Properties
		public HashSet<QueryMethod> AdditionalQueryMethods { get; set; }

		public List<FacetQuery> FacetQueries { get; set; }

		public BaseQuery FilterQuery { get; set; }

		public List<IFieldQueryTranslator> VirtualFieldProcessors { get; set; }
	}
	
}

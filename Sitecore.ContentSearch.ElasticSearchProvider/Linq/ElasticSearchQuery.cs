using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Methods;
using Nest;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Linq
{
	public class ElasticSearchQuery : IDumpable
	{
		public List<FacetQuery> FacetQueries { get; protected set; }
		public BaseQuery Filter { get; protected set; }
		public List<QueryMethod> Methods { get; protected set; }
		public BaseQuery Query { get; protected set; }
		public List<IFieldQueryTranslator> VirtualFieldProcessors { get; protected set; }
		public ElasticSearchQueryMapperState State { get; protected set; }

		public ElasticSearchQuery(BaseQuery query, BaseQuery filterQuery, IEnumerable<QueryMethod> methods, IEnumerable<IFieldQueryTranslator> virtualFieldProcessors, IEnumerable<FacetQuery> facetQueries)
		{
			Query = query;
			Filter = filterQuery;
			Methods = methods.ToList();
			VirtualFieldProcessors = virtualFieldProcessors.ToList();
			FacetQueries = (facetQueries != null) ? facetQueries.ToList() : new List<FacetQuery>();
		}

		public void Dump(TextWriter writer)
		{
			if (Query != null)
			{
				writer.WriteLine("Query: {0}", Query); //TODO: Query.ToString() will simply return the type name, it doesn't serialize the query
			}
			writer.WriteLine("Method count: {0}", Methods.Count);
			for (var i = 0; i < Methods.Count; i++)
			{
				writer.WriteLine("  Method[{0}]: {1}: {2}", i, Methods[i].MethodType, Methods[i]);
			}
			if (FacetQueries.Count > 0)
			{
				writer.WriteLine("Facet query count: {0}", FacetQueries.Count);
				foreach (var query in FacetQueries)
				{
					writer.WriteLine("  FacetQuery: {0}", query);
				}
			}
		}

		public override string ToString()
		{
			return Query.ToString(); //TODO: Query.ToString() will simply return the type name, it doesn't serialize the query
		}
	}
}

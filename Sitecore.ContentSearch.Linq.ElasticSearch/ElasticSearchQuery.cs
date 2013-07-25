using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Methods;

namespace Sitecore.ContentSearch.Linq.ElasticSearch
{
	public class ElasticSearchQuery : IDumpable
	{
		public List<FacetQuery> FacetQueries { get; protected set; }
		public AbstractSolrQuery Filter { get; protected set; }
		public List<QueryMethod> Methods { get; protected set; }
		public AbstractSolrQuery Query { get; protected set; }
		public List<IFieldQueryTranslator> VirtualFieldProcessors { get; protected set; }

		public ElasticSearchQuery(AbstractSolrQuery query, AbstractSolrQuery filterQuery, IEnumerable<QueryMethod> methods, IEnumerable<IFieldQueryTranslator> virtualFieldProcessors, IEnumerable<FacetQuery> facetQueries)
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
				writer.WriteLine("Query: {0}", this.Query);
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
			DefaultQuerySerializer serializer = new DefaultQuerySerializer(new DefaultFieldSerializer());
			return serializer.Serialize(Query);
		}
	}
}

using System;
using System.Collections.Generic;
using Nest;
using Sitecore.ContentSearch.Maintenance;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchIndexSummary : ISearchIndexSummary
	{
		private readonly ElasticSearchIndex _index;
		private readonly ElasticClient _esClient;

		public ElasticSearchIndexSummary(ElasticSearchIndex index, ElasticClient esClient)
		{
			_index = index;
			_esClient = esClient;
		}

		public long NumberOfDocuments
		{
			get
			{
				var stats = _esClient.Stats(_index.Name);
				return stats.Stats.Total.Documents.Count;
			}
		}

		public DateTime LastUpdated
		{
			get
			{
				if (_index.PropertyStore == null)
				{
					return DateTime.MinValue;
				}
				var isoDate = _index.PropertyStore.Get(IndexProperties.LastUpdatedKey);
				if (isoDate.Length == 0)
				{
					var datetime = DateTime.Now.Subtract(new TimeSpan(0, 12, 0, 0));
					_index.PropertyStore.Set(IndexProperties.LastUpdatedKey, DateUtil.ToIsoDate(datetime));
					return datetime;
				}
				return DateUtil.IsoDateToDateTime(isoDate, DateTime.MinValue);
			}
			set
			{
				if (_index.PropertyStore != null)
				{
					_index.PropertyStore.Set(IndexProperties.LastUpdatedKey, DateUtil.ToIsoDate(value));
				}
			}

		}

		public string Directory
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsOptimized
		{
			get { throw new NotImplementedException(); }
		}

		public bool HasDeletions
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsHealthy
		{
			get { throw new NotImplementedException(); }
		}

		public int NumberOfFields
		{
			get { throw new NotImplementedException(); }
		}

		public long NumberOfTerms
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsClean
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsMissingSegment
		{
			get { throw new NotImplementedException(); }
		}

		public int NumberOfBadSegments
		{
			get { throw new NotImplementedException(); }
		}

		public bool OutOfDateIndex
		{
			get { throw new NotImplementedException(); }
		}

		public IDictionary<string, string> UserData
		{
			get { throw new NotImplementedException(); }
		}


	}
}

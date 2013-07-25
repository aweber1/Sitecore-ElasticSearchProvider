using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Diagnostics;
using Sitecore.Events;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchUpdateContext : IProviderUpdateContext
	{
		// Fields
		private readonly ICommitPolicyExecutor _commitPolicyExecutor;
		private readonly ISearchIndex _index;
		private readonly ElasticClient _esClient;
		private readonly List<Dictionary<string, object>> _documentsToAddOrUpdate;
		private readonly List<string> _documentIdsToDelete;

		// Properties
		public ICommitPolicy CommitPolicy { get; private set; }

		public ISearchIndex Index
		{
			get { return _index; }
		}

		public bool IsParallel { get; private set; }

		public ParallelOptions ParallelOptions { get; private set; }

		// Methods
		public ElasticSearchUpdateContext(ElasticSearchIndex index, ElasticClient esClient)
		{
			Assert.ArgumentNotNull(index, "index");

			_index = index;
			_esClient = esClient;
			_documentsToAddOrUpdate = new List<Dictionary<string, object>>();
			_documentIdsToDelete = new List<string>();

			IsParallel = ContentSearchConfigurationSettings.IsParallelIndexingEnabled;
			ParallelOptions = new ParallelOptions();
			var parallelIndexingCoreLimit = ContentSearchConfigurationSettings.ParallelIndexingCoreLimit;
			if (parallelIndexingCoreLimit > 0)
			{
				ParallelOptions.MaxDegreeOfParallelism = parallelIndexingCoreLimit;
			}
			_commitPolicyExecutor = new NullCommitPolicyExecutor();
		}

		public ElasticSearchUpdateContext(ElasticSearchIndex elasticSearchIndex, ElasticClient esClient, ICommitPolicy commitPolicy, ICommitPolicyExecutor commitPolicyExecutor)
			: this(elasticSearchIndex, esClient)
		{
			if (commitPolicy != null && commitPolicyExecutor == null)
			{
				throw new ArgumentNullException("commitPolicyExecutor");
			}
			CommitPolicy = commitPolicy;
			_commitPolicyExecutor = (commitPolicy != null) ? commitPolicyExecutor : new NullCommitPolicyExecutor();
		}

		public void AddDocument(object itemToAddOrUpdate, IExecutionContext executionContext)
		{
			Assert.ArgumentNotNull(itemToAddOrUpdate, "itemtoAdd");

			_documentsToAddOrUpdate.Add(itemToAddOrUpdate as Dictionary<string, object>);

			var job = Context.Job;
			if (job == null || job.Category != "index")
				return;

			var status = job.Status;
			status.Processed = status.Processed + 1;

			_commitPolicyExecutor.IndexModified(this, IndexOperation.Add);
		}

		//no concept of "Commit" in ES... need to determine what to do with this method
		//perhaps we just use the add document method to add to a collection, then use commit to index the collection?
		public void Commit()
		{
			CrawlingLog.Log.Info(string.Format("[Index={0}] Commit", _index.Name));
			Event.RaiseEvent("indexing:committing", new object[] { _index.Name });

			if (_documentsToAddOrUpdate.Count > 0)
			{
				CommitAddOrUpdate();
			}

			if (_documentIdsToDelete.Count > 0)
			{
				CommitDelete();
			}

			_commitPolicyExecutor.Committed(this);

			Event.RaiseEvent("indexing:committed", new object[] { _index.Name });
		}

		protected virtual void CommitAddOrUpdate()
		{
			//because we're specifying our own value for the _id field, we need to specifically declare the _id to the API when indexing.
			//otherwise ES will try to use it's own value for the _id field, and a parsing exception will be thrown because the ES _id and our _id don't match
			//tried to use .IndexMany, but there wasn't an obvious way to project our _id field value to each document on index
			//using the BulkDescription, however, appears to work. doesn't seem terribly pretty as opposed to using .IndexMany, but it's a valid way to project the _id field during a bulk operation
			var descriptor = new BulkDescriptor();
			foreach (var document in _documentsToAddOrUpdate)
			{
				var doc = document;
				descriptor.Index<object>(indexDescriptor => indexDescriptor.Object(doc).Id(doc["_id"].ToString()).Index(_index.Name));
			}

			var response = _esClient.Bulk(descriptor);
			if (response.Items != null)
				CrawlingLog.Log.Info(string.Format("[Index={0}] Commit added or updated {1} documents in {2}ms", _index.Name, response.Items.Count(), response.Took));
		}

		protected virtual void CommitDelete()
		{
			var response = _esClient.DeleteByQuery(delegate(RoutingQueryPathDescriptor<object> pathDescriptor)
			{
				pathDescriptor.Index(_index.Name);
				foreach (var documentId in _documentIdsToDelete)
				{
					var docId = documentId;
					//descriptor.Delete<object>(indexDescriptor => indexDescriptor.Id(docId).Index(_index.Name));
					//var q = Query.Term("_group", docId);
					pathDescriptor.Term("_group", docId);
				}
			});

			if (response.OK)
				CrawlingLog.Log.Info(string.Format("[Index={0}] Commit deleted {1} documents", _index.Name, _documentIdsToDelete.Count));
			//var descriptor = new BulkDescriptor();
			//var response = _esClient.Bulk(descriptor);
			//if (response.Items != null)
			//	CrawlingLog.Log.Info(string.Format("[Index={0}] Commit deleted {1} documents in {2}ms", _index.Name, response.Items.Count(), response.Took));
		}

		public void Delete(IIndexableId id)
		{
			Assert.ArgumentNotNull(id, "id");

			var formattedId = _index.Configuration.IndexFieldStorageValueFormatter.FormatValueForIndexStorage(id);
			_documentIdsToDelete.Add(formattedId.ToString());

			_commitPolicyExecutor.IndexModified(this, IndexOperation.Delete);
		}

		public void Delete(IIndexableUniqueId id)
		{
			Assert.ArgumentNotNull(id, "id");
			//_esClient.DeleteByQuery()
			//_esClient.DeleteById(id.ToString(), new DeleteParameters {OpType = OpType.None, Refresh = true});
			//_solr.Delete(_index.Configuration.IndexFieldStorageValueFormatter.FormatValueForIndexStorage(id).ToString());
			_commitPolicyExecutor.IndexModified(this, IndexOperation.Delete);
		}

		public void Dispose()
		{
		}

		public void Optimize()
		{
			CrawlingLog.Log.Info(string.Format("[Index={0}] Optimize", _index.Name));
			Event.RaiseEvent("indexing:optimizing", new object[] { _index.Name });
			_esClient.Optimize(_index.Name);
			Event.RaiseEvent("indexing:optimized", new object[] { _index.Name });
		}

		public void UpdateDocument(object itemToUpdate, object criteriaForUpdate, IExecutionContext executionContext)
		{
			AddDocument(itemToUpdate, executionContext);
		}
	}
}

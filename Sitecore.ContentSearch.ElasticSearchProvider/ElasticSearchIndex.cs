using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;
using Nest;
using Sitecore.ContentSearch.Events;
using Sitecore.ContentSearch.Exceptions;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.Maintenance.Strategies;
using Sitecore.ContentSearch.Security;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using Sitecore.Events;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchIndex : ISearchIndex
	{
		private ElasticClient _client;
		private bool _initialized;

		public ElasticSearchIndex(string name, IIndexPropertyStore propertyStore)
		{
			Assert.ArgumentNotNull(name, "name");

			Name = name;
			Crawlers = new List<IProviderCrawler>();
			Strategies = new List<IIndexUpdateStrategy>();
			PropertyStore = propertyStore;
			
			//Summary property needs to be set in the constructor. 
			//Don't set it in the Initialize method, as that method isn't called every time an index is instantiated.
			Summary = new ElasticSearchIndexSummary(this, Client);
			
		}

		public string Name { get; private set; }
		public ISearchIndexSummary Summary { get; private set; }

		public ISearchIndexSchema Schema
		{
			get { throw new NotImplementedException();}
		}
		
		public IIndexPropertyStore PropertyStore { get; private set; }
		public AbstractFieldNameTranslator FieldNameTranslator { get; private set; }
		public ProviderIndexConfiguration Configuration { get; set; }
		public List<IIndexUpdateStrategy> Strategies { get; private set; }
		public List<IProviderCrawler> Crawlers { get; private set; }

		public IIndexOperations Operations
		{
			get { return new ElasticSearchIndexOperations(this); }
		}

		public ElasticClient Client
		{
			get
			{
				if (_client == null)
				{
					var uri = new Uri(ElasticSearchContentSearchManager.ServiceAddress);
					var settings = new ConnectionSettings(uri).SetDefaultIndex(Name);
					_client = new ElasticClient(settings);
				}
				return _client;
			}
		}

		public void Initialize()
		{
			if (!Client.IsValid)
			{
				var message = "Unable to connect to [" + Client.Settings.Uri + "]";
				Log.Error(message, this);
				throw new SearchProviderConnectionException(message, SupportedProviders.ElasticSearch);
			}

			foreach (var crawler in Crawlers)
			{
				crawler.Initialize(this);
			}
			foreach (var strategy in Strategies)
			{
				strategy.Initialize(this);
			}

			var configuration = Configuration as ElasticSearchIndexConfiguration;
			if (configuration == null)
			{
				throw new ConfigurationErrorsException("Index has no configuration.");
			}

			var indexDocumentPropertyMapper = configuration.IndexDocumentPropertyMapper as ISearchIndexInitializable;
			if (indexDocumentPropertyMapper != null)
			{
				indexDocumentPropertyMapper.Initialize(this);
			}

			FieldNameTranslator = new ElasticSearchFieldNameTranslator(this);

			_initialized = true;
		}

		protected void EnsureInitialized()
		{
			Assert.IsNotNull(Configuration, "Configuration");
			Assert.IsTrue(Configuration is ElasticSearchIndexConfiguration, "Configuration type is not expected.");
			if (!_initialized)
			{
				throw new InvalidOperationException("Index has not been initialized.");
			}
		}

		public void AddCrawler(IProviderCrawler crawler)
		{
			crawler.Initialize(this);
			Crawlers.Add(crawler);
		}

		public void AddStrategy(IIndexUpdateStrategy strategy)
		{
			strategy.Initialize(this);
			Strategies.Add(strategy);
		}

		public void SetCommitPolicy(XmlNode configNode)
		{
			throw new NotImplementedException();
		}

		public void SetCommitPolicyExecutor(XmlNode configNode)
		{
			throw new NotImplementedException();
		}

		public void Rebuild()
		{
			Event.RaiseEvent("indexing:start", new object[] { Name, true });
			
			var indexingStartEvent = new IndexingStartedEvent
			{
				IndexName = Name,
				FullRebuild = true
			};
			
			EventManager.QueueEvent(indexingStartEvent);
			
			Client.DeleteIndex(Name);

			using (var context = CreateUpdateContext())
			{
				foreach (var crawler in Crawlers)
				{
					crawler.RebuildFromRoot(context);
				}
				context.Commit();
				context.Optimize();
			}

			Event.RaiseEvent("indexing:end", new object[] { Name, true });
			var indexingEndEvent = new IndexingFinishedEvent
			{
				IndexName = Name,
				FullRebuild = true
			};
			EventManager.QueueEvent(indexingEndEvent);
		}

		public void Refresh(IIndexable indexableStartingPoint)
		{
			using (var context = CreateUpdateContext())
			{
				foreach (var crawler in Crawlers)
				{
					crawler.RefreshFromRoot(context, indexableStartingPoint);
				}
				context.Commit();
				context.Optimize();
			}
		}

		public void Update(IIndexableUniqueId indexableUniqueId)
		{
			using (var context = CreateUpdateContext())
			{
				foreach (var crawler in Crawlers)
				{
					crawler.Update(context, indexableUniqueId);
				}
				context.Commit();
			}
		}

		public void Update(IEnumerable<IIndexableUniqueId> indexableUniqueIds)
		{
			throw new NotImplementedException();
		}

		public void Delete(IIndexableId indexableId)
		{
			using (var context = CreateUpdateContext())
			{
				foreach (var crawler in Crawlers)
				{
					crawler.Delete(context, indexableId);
				}
				context.Commit();
			}
		}

		public void Delete(IIndexableUniqueId indexableUniqueId)
		{
			throw new NotImplementedException();
		}

		

		public IProviderUpdateContext CreateUpdateContext()
		{
			return new ElasticSearchUpdateContext(this, Client);
		}

		public IProviderDeleteContext CreateDeleteContext()
		{
			throw new NotImplementedException();
		}

		public IProviderSearchContext CreateSearchContext(SearchSecurityOptions options = SearchSecurityOptions.EnableSecurityCheck)
		{
			return new ElasticSearchContext(this, options);
		}
	}
}

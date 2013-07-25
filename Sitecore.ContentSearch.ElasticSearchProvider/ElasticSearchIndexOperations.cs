using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Pipelines.IndexingFilters;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Globalization;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchIndexOperations : IIndexOperations
	{
		// Fields
		private readonly ElasticSearchIndex _index;

		public ElasticSearchIndexOperations(ElasticSearchIndex index)
		{
			Assert.ArgumentNotNull(index, "index");
			_index = index;
		}

		public void Add(IIndexable indexable, IProviderUpdateContext context, ProviderIndexConfiguration indexConfiguration)
		{
			Assert.ArgumentNotNull(indexable, "indexable");
			Assert.ArgumentNotNull(context, "context");
			Assert.ArgumentNotNull(indexConfiguration, "indexConfiguration");
			
			Item item = indexable as SitecoreIndexableItem;
			if (item == null)
				return; //log or assert this

			if (context.IsParallel)
			{
				Action<Language> body = language => ProcessLanguageItem((SitecoreIndexableItem)item, language, context);
				Parallel.ForEach(item.Languages, context.ParallelOptions, body);
			}
			else
			{
				foreach (var language in item.Languages)
				{
					ProcessLanguageItem((SitecoreIndexableItem)item, language, context);
				}
			}
		}

		public void Delete(IIndexable indexable, IProviderUpdateContext context)
		{
			Assert.ArgumentNotNull(indexable, "indexable");
			Assert.ArgumentNotNull(context, "context");
			Delete(indexable.Id, context);
		}

		public void Delete(IIndexableId id, IProviderUpdateContext context)
		{
			Assert.ArgumentNotNull(id, "id");
			Assert.ArgumentNotNull(context, "context");
			context.Delete(id);
		}

		public void Delete(IIndexableUniqueId id, IProviderUpdateContext context)
		{
			Assert.ArgumentNotNull(id, "id");
			Assert.ArgumentNotNull(context, "context");
			context.Delete(id);
		}

		public void Update(IIndexable indexable, IProviderUpdateContext context, ProviderIndexConfiguration indexConfiguration)
		{
			Assert.ArgumentNotNull(indexable, "indexable");
			Add(indexable, context, indexConfiguration);
		}

		private void ProcessLanguageItem(IIndexable indexable, Language language, IProviderUpdateContext context)
		{
			Item item = indexable as SitecoreIndexableItem;
			if (item == null)
				return;

			var latestVersion = item.Database.GetItem(item.ID, language, Data.Version.Latest);
			if (latestVersion == null)
			{
				CrawlingLog.Log.Warn(string.Format("ElasticIndexOperations : AddItem : Latest version not found for item {0}. Skipping.", item.Uri));
			}
			else
			{
				var versions = latestVersion.Versions.GetVersions(false);
				if (context.IsParallel)
				{
					Action<Item> body = delegate(Item version)
						{
							try
							{
								ApplyPermissionsThenIndex(context, (SitecoreIndexableItem)version, (SitecoreIndexableItem)latestVersion);
							}
							catch (Exception exception)
							{
								CrawlingLog.Log.Warn(string.Format("ElasticIndexOperations : AddItem : Could not build document data {0}. Skipping.", version.ID.Guid), exception);
							}
						};
					Parallel.ForEach(versions, context.ParallelOptions, body);
				}
				else
				{
					foreach (var version in versions)
					{
						ApplyPermissionsThenIndex(context, (SitecoreIndexableItem)version, (SitecoreIndexableItem)latestVersion);
					}
				}
			}
		}

		private void ApplyPermissionsThenIndex(IProviderUpdateContext context, IIndexable version, IIndexable latestVersion)
		{
			if (InboundIndexFilterPipeline.Run(new InboundIndexFilterArgs(version)))
			{
				Event.RaiseEvent("indexing:excludedfromindex", new object[] { _index.Name, version.UniqueId });
			}
			else
			{
				var itemToAdd = IndexVersion(version, latestVersion, context);
				if (itemToAdd == null)
				{
					CrawlingLog.Log.Warn(string.Format("ElasticIndexOperations : AddItem : IndexVersion produced a NULL doc for version {0}. Skipping.", version.UniqueId));
				}
				else
				{
					context.AddDocument(itemToAdd, null);
				}
			}
		}

		internal Dictionary<string, object> IndexVersion(IIndexable indexable, IIndexable latestVersion, IProviderUpdateContext context)
		{
			Assert.ArgumentNotNull(indexable, "indexable");
			Assert.ArgumentNotNull(latestVersion, "latestVersion");
			
			var documentOptions = _index.Configuration.DocumentOptions as ElasticSearchDocumentBuilderOptions;
			Assert.Required(documentOptions, "IDocumentBuilderOptions of wrong type for this crawler");
			
			if (indexable.Id.ToString() == string.Empty)
			{
				return new Dictionary<string, object>();
			}

			var fields = indexable as IIndexableBuiltinFields;
			if (fields != null && latestVersion is SitecoreIndexableItem)
			{
				fields.IsLatestVersion = fields.Version == ((SitecoreIndexableItem)latestVersion).Item.Version.Number;
			}

			var item = indexable as SitecoreIndexableItem;
			if (item != null)
			{
				item.IndexFieldStorageValueFormatter = context.Index.Configuration.IndexFieldStorageValueFormatter;
			}

			var builder = new ElasticSearchDocumentBuilder(indexable, context);
			builder.AddSpecialField("_uniqueid", indexable.UniqueId.Value);
			builder.AddSpecialField("_datasource", indexable.DataSource); //do not lowercase this value, the check security pipeline evaluates the _datasource field using a case-sensitive check
			builder.AddSpecialField("_indexname", _index.Name.ToLower());
			builder.AddSpecialFields();
			builder.AddItemFields();
			//builder.AddComputedIndexFields();
			//builder.AddCopyFields();
			builder.AddBoost();

			return builder.Document.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}
	}
}

using System.Collections.Concurrent;
using System.Globalization;
using Sitecore.ContentSearch.Boosting;
using Sitecore.ContentSearch.ComputedFields;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchDocumentBuilder : AbstractDocumentBuilder<ConcurrentDictionary<string, object>>
	{
		// Fields
		private readonly CultureInfo _culture;
		private readonly ElasticSearchFieldMap _fieldMap;

		// Methods
		public ElasticSearchDocumentBuilder(IIndexable indexable, IProviderUpdateContext context) : base(indexable, context)
		{
			_fieldMap = context.Index.Configuration.FieldMap as ElasticSearchFieldMap;
			//this.fieldNameTranslator = context.Index.FieldNameTranslator as SolrFieldNameTranslator;
			_culture = indexable.Culture;
		}

		public override void AddBoost()
		{
			var num = BoostingManager.ResolveItemBoosting(Indexable);
			if (num > 0)
			{
				Document.GetOrAdd("_documentBoost", num);
			}
		}

		public void AddComputedIndexFields()
		{
			foreach (var field in Options.ComputedIndexFields)
			{
				if (!string.IsNullOrEmpty(field.ReturnType) && !Index.Schema.AllFieldNames.Contains(field.FieldName))
				{
					AddField(field.FieldName, field.ComputeFieldValue(Indexable), field.ReturnType);
				}
				else
				{
					AddField(field.FieldName, field.ComputeFieldValue(Indexable));
				}
			}
		}

		public override void AddField(IIndexableDataField field)
		{
			var fieldName = field.Name;
			var fieldValue = Index.Configuration.FieldReaders.GetFieldValue(field);
			if (fieldValue == null || (fieldValue is string && string.IsNullOrEmpty(fieldValue.ToString()))) 
				return;

			var num = BoostingManager.ResolveFieldBoosting(field);
			//name = this.fieldNameTranslator.GetIndexFieldName(name, fieldValue.GetType(), this._culture);
			if (!IsMedia && IndexOperationsHelper.IsTextField(field))
			{
				StoreField(BuiltinFields.Content, fieldValue, true, null);
			}
			StoreField(fieldName, fieldValue, false, num);
		}

		public override void AddField(string fieldName, object fieldValue, bool append = false)
		{
			if (fieldValue != null && (!(fieldValue is string) || !string.IsNullOrEmpty(fieldValue.ToString())))
			{
				//fieldName = this.fieldNameTranslator.GetIndexFieldName(fieldName, fieldValue.GetType(), this._culture);
				StoreField(fieldName, fieldValue, append, null);
			}
		}

		private void AddField(string fieldName, object fieldValue, string returnType)
		{
			if (fieldValue != null && (!(fieldValue is string) || !string.IsNullOrEmpty(fieldValue.ToString())))
			{
				//fieldName = this.fieldNameTranslator.GetIndexFieldName(fieldName, returnType, this._culture);
				StoreField(fieldName, fieldValue, false, null);
			}
		}

		private void StoreField(string fieldName, object fieldValue, bool append = false, float? boost = new float?())
		{
			if (Index.Configuration.IndexFieldStorageValueFormatter != null)
			{
				fieldValue = Index.Configuration.IndexFieldStorageValueFormatter.FormatValueForIndexStorage(fieldValue);
			}

			if ((append && Document.ContainsKey(fieldName)) && (fieldValue is string))
			{
				ConcurrentDictionary<string, object> dictionary;
				string str;
				(dictionary = Document)[str = fieldName] = dictionary[str] + " " + ((string)fieldValue);
			}

			if (Document.ContainsKey(fieldName)) 
				return;

			if (boost.HasValue)
			{
				if (boost.GetValueOrDefault() > 0)
				{
					fieldValue = new ElasticSearchBoostedField(fieldValue, boost);
				}
			}
			Document.GetOrAdd(fieldName, fieldValue);
			//if (this.fieldNameTranslator.HasCulture(fieldName) && !Settings.DefaultLanguage.StartsWith(_culture.TwoLetterISOLanguageName))
			//{
			//	Document.GetOrAdd(this.fieldNameTranslator.StripKnownCultures(fieldName), fieldValue);
			//}
		}
	}
}

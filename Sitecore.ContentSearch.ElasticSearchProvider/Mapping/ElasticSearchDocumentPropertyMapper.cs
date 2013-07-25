using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.Diagnostics;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Mapping
{
	public class ElasticSearchDocumentPropertyMapper : DefaultDocumentMapper<Dictionary<string, object>>
	{
		// Methods
		protected override IEnumerable<string> GetDocumentFieldNames(Dictionary<string, object> document)
		{
			return document.Keys;
		}

		protected override void ReadDocumentFields<TElement>(Dictionary<string, object> document, IEnumerable<string> fieldNames, DocumentTypeMapInfo documentTypeMapInfo, IEnumerable<IFieldQueryTranslator> virtualFieldProcessors, TElement result)
		{
			Assert.ArgumentNotNull(document, "document");
			Assert.ArgumentNotNull(documentTypeMapInfo, "documentTypeMapInfo");
			Assert.ArgumentNotNull(result, "result");

			if (virtualFieldProcessors != null)
			{
				Func<Dictionary<string, object>, IFieldQueryTranslator, Dictionary<string, object>> func =
					(current, processor) =>
					(Dictionary<string, object>) processor.TranslateFieldResult(current, index.FieldNameTranslator);
				document = virtualFieldProcessors.Aggregate(document, func);
			}

			if (index.FieldNameTranslator != null)
			{
				foreach (var pair in index.FieldNameTranslator.MapDocumentFieldsToType(result.GetType(), fieldNames))
				{
					object obj2 = null;
					if (document.ContainsKey(pair.Key))
					{
						obj2 = document[pair.Key];
					}
					var key = pair.Key;
					foreach (var str2 in pair.Value)
					{
						documentTypeMapInfo.SetProperty(result, str2, key, obj2);
					}
				}
			}
			else
			{
				foreach (var pair2 in document)
				{
					documentTypeMapInfo.SetProperty(result, pair2.Key, pair2.Key, pair2.Value);
				}
			}
		}

	}
}

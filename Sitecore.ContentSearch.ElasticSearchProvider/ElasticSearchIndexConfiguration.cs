using System;
using System.Collections.Generic;
using System.Xml;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.Diagnostics;
using Sitecore.Reflection;
using Sitecore.Xml;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchIndexConfiguration : ProviderIndexConfiguration
	{
		// Properties
		public IIndexDocumentPropertyMapper<Dictionary<string, object>> IndexDocumentPropertyMapper { get; set; }

		// Methods
		public ElasticSearchIndexConfiguration()
		{
			DocumentOptions = new ElasticSearchDocumentBuilderOptions();
		}

		public override void AddComputedIndexField(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");
			var fieldName = XmlUtil.GetAttribute("fieldName", configNode, true);
			var fieldType = XmlUtil.GetValue(configNode);
			if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(fieldType))
			{
				throw new InvalidOperationException("Could not parse computed index field entry: " + configNode.OuterXml);
			}
			var field = ReflectionUtil.CreateObject(fieldType) as IComputedIndexField;
			if (field == null) 
				return;

			field.FieldName = fieldName.ToLowerInvariant();
			var returnType = XmlUtil.GetAttribute("returnType", configNode, true);
			if (FieldMap != null && !string.IsNullOrWhiteSpace(returnType))
			{
				field.ReturnType = returnType;
				((ElasticSearchFieldMap)FieldMap).AddFieldByFieldName(field.FieldName, field.ReturnType.ToLowerInvariant());
			}
			DocumentOptions.ComputedIndexFields.Add(field);
		}


		//public void AddCopyField(XmlNode configNode)
		//{
		//	Assert.ArgumentNotNull(configNode, "configNode");
		//	string str = XmlUtil.GetAttribute("fieldName", configNode, true);
		//	string str2 = XmlUtil.GetAttribute("returnType", configNode, true);
		//	string str3 = XmlUtil.GetAttribute("format", configNode, true);
		//	string str4 = XmlUtil.GetValue(configNode);
		//	if (((str != null) && (str2 != null)) && (str4 != null))
		//	{
		//		SolrCopyField field = new SolrCopyField
		//		{
		//			FieldName = str,
		//			Format = str3,
		//			ReturnType = str2,
		//			SourceField = str4
		//		};
		//		((SolrFieldMap)base.FieldMap).AddFieldByFieldName(field.FieldName, field.ReturnType.ToLowerInvariant());
		//		((SolrDocumentBuilderOptions)base.DocumentOptions).CopyFields.Add(field);
		//	}
		//}

		
	}
}

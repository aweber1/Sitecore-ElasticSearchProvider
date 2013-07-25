using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Reflection;
using Sitecore.Xml;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchFieldMap : IFieldMap
	{
		// Fields
		private readonly Dictionary<string, ElasticSearchFieldConfiguration> _fieldNameMap = new Dictionary<string, ElasticSearchFieldConfiguration>();
		private readonly Dictionary<string, ElasticSearchFieldConfiguration> _fieldTypeNameMap = new Dictionary<string, ElasticSearchFieldConfiguration>();
		private readonly ElasticSearchFieldConfiguration _elasticSearchFieldDefault = new ElasticSearchFieldConfiguration();
		private readonly Dictionary<string, ElasticSearchFieldConfiguration> _typeMap = new Dictionary<string, ElasticSearchFieldConfiguration>();

		// Properties
		public List<ElasticSearchFieldConfiguration> AvailableTypes { get; set; }

		// Methods
		public ElasticSearchFieldMap()
		{
			AvailableTypes = new List<ElasticSearchFieldConfiguration>();
		}

		public void AddFieldByFieldName(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");
			string attribute = XmlUtil.GetAttribute("fieldName", configNode);
			string str2 = XmlUtil.GetAttribute("returnType", configNode);
			if ((attribute == null) || (str2 == null))
			{
				throw new ConfigurationException("Unable to process 'AddFieldByFieldName' config section.");
			}
			AddFieldByFieldName(attribute.ToLowerInvariant(), str2.ToLowerInvariant());
		}

		public void AddFieldByFieldName(string fieldName, ElasticSearchFieldConfiguration setting)
		{
			_fieldNameMap[fieldName.ToLowerInvariant()] = setting;
		}

		public void AddFieldByFieldName(string fieldName, string returnType)
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, "fieldName");
			Assert.ArgumentNotNullOrEmpty(returnType, "returnType");
			if (_typeMap.Count == 0 || !_typeMap.ContainsKey(returnType.ToLowerInvariant()))
				return;

			var configuration = _typeMap[returnType.ToLowerInvariant()];
			_fieldNameMap[fieldName.ToLowerInvariant()] = configuration;
		}

		public void AddFieldByFieldTypeName(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");

			var fieldTypeName = XmlUtil.GetAttribute("fieldTypeName", configNode);
			var returnType = XmlUtil.GetAttribute("returnType", configNode);
			if (fieldTypeName == null || returnType == null)
			{
				throw new ConfigurationException("Unable to process 'AddFieldByFieldName' config section.");
			}

			AddFieldByFieldTypeName(fieldTypeName.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries), returnType);
		}

		public void AddFieldByFieldTypeName(IEnumerable<string> fieldTypeNames, string returnType)
		{
			Assert.ArgumentNotNull(fieldTypeNames, "fieldTypeNames");
			Assert.ArgumentNotNullOrEmpty(returnType, "returnType");
			
			if (_typeMap.Count == 0 || !_typeMap.ContainsKey(returnType.ToLowerInvariant()))
				return;

			var configuration = _typeMap[returnType.ToLowerInvariant()];
			foreach (var fieldTypeName in fieldTypeNames)
			{
				_fieldTypeNameMap[fieldTypeName.ToLowerInvariant()] = configuration;
			}
		}

		public void AddTypeMatch(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");

			var settingType = XmlUtil.GetAttribute("settingType", configNode);
			var typeName = XmlUtil.GetAttribute("typeName", configNode);
			var xmlAttributes = XmlUtil.GetAttributes(configNode);

			if (settingType == null || xmlAttributes == null || typeName == null)
			{
				throw new ConfigurationException("Unable to process 'AddTypeMatch' config section.");
			}

			var typeInfo = ReflectionUtil.GetTypeInfo(settingType);
			var attributes = xmlAttributes.Keys.Cast<string>().ToDictionary<string, string, string>(attribute => attribute, attribute => xmlAttributes[attribute]);
			AddTypeMatch(typeName, typeInfo, attributes);
		}

		public void AddTypeMatch(string typeName, Type settingType, IDictionary<string, string> attributes)
		{
			Assert.ArgumentNotNullOrEmpty(typeName, "typeName");
			Assert.ArgumentNotNull(settingType, "settingType");

			var objArray = new object[3];
			objArray[0] = typeName;
			objArray[1] = attributes;
			var configuration = (ElasticSearchFieldConfiguration)Activator.CreateInstance(settingType, objArray);
			Assert.IsNotNull(configuration, string.Format("Unable to create : {0}", settingType));

			_typeMap[typeName.ToLowerInvariant()] = configuration;
			AvailableTypes.Add(configuration);
		}

		public AbstractSearchFieldConfiguration GetFieldConfiguration(IIndexableDataField field)
		{
			Assert.ArgumentNotNull(field, "field");

			ElasticSearchFieldConfiguration configuration;
			if (_fieldNameMap.TryGetValue(field.Name.ToLowerInvariant(), out configuration))
			{
				return configuration;
			}
			if (_fieldTypeNameMap.TryGetValue(field.TypeKey.ToLowerInvariant(), out configuration))
			{
				return configuration;
			}
			return _elasticSearchFieldDefault;
		}

		public AbstractSearchFieldConfiguration GetFieldConfiguration(string fieldName)
		{
			Assert.ArgumentNotNull(fieldName, "fieldName");

			ElasticSearchFieldConfiguration configuration;
			return _fieldNameMap.TryGetValue(fieldName.ToLowerInvariant(), out configuration) ? configuration : null;
		}

		public AbstractSearchFieldConfiguration GetFieldConfiguration(Type returnType)
		{
			Assert.ArgumentNotNull(returnType, "returnType");

			var source = (from x in AvailableTypes
						  where x.SystemType == returnType
						  select x).ToArray<ElasticSearchFieldConfiguration>();
			return source.FirstOrDefault();
		}

		public AbstractSearchFieldConfiguration GetFieldConfigurationByFieldTypeName(string fieldTypeName)
		{
			Assert.ArgumentNotNull(fieldTypeName, "fieldTypeName");

			ElasticSearchFieldConfiguration configuration;
			return _fieldTypeNameMap.TryGetValue(fieldTypeName.ToLowerInvariant(), out configuration) ? configuration : null;
		}

		public AbstractSearchFieldConfiguration GetFieldConfigurationByReturnType(string returnType)
		{
			Assert.ArgumentNotNull(returnType, "returnType");

			ElasticSearchFieldConfiguration configuration;
			return _typeMap.TryGetValue(returnType.ToLowerInvariant(), out configuration) ? configuration : null;
		}
	}
}

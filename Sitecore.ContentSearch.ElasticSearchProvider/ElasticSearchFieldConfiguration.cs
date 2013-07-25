using System;
using System.Collections.Generic;
using System.Xml;
using Sitecore.Configuration;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchFieldConfiguration : AbstractSearchFieldConfiguration
	{
		// Properties
		public float Boost { get; private set; }
		public string CultureFormat { get; private set; }
		public string DefaultValue { get; private set; }
		public string FieldNameFormat { get; private set; }
		public bool Indexed { get; private set; }
		public bool? MultiValued { get; private set; }
		public bool? OmitNorms { get; private set; }
		public bool? Required { get; private set; }
		public string SchemaType { get; private set; }
		public bool Stored { get; private set; }
		public Type SystemType { get; private set; }
		public bool? TermOffsets { get; private set; }
		public bool? TermPositions { get; private set; }
		public bool? TermVectors { get; private set; }
		protected string TypeName { get; private set; }

		// Methods
		public ElasticSearchFieldConfiguration()
		{
			Indexed = true;
			Stored = false;
			Boost = 1f;
			FieldNameFormat = "{0}";
			SystemType = typeof(string);
		}

		public ElasticSearchFieldConfiguration(string typeName, IDictionary<string, string> attributes, XmlNode configNode) : base(null, null, attributes, configNode)
		{
			FieldNameFormat = "{0}";
			TypeName = typeName;
			foreach (var pair in attributes)
			{
				switch (pair.Key.ToLowerInvariant())
				{
					case "termoffsets":
						bool flag;
						if (bool.TryParse(pair.Value, out flag))
							TermOffsets = flag;
						break;

					case "termpositions":
						bool flag2;
						bool.TryParse(pair.Value, out flag2);
						TermPositions = flag2;
						break;

					case "termvectors":
						bool flag3;
						bool.TryParse(pair.Value, out flag3);
						TermVectors = flag3;
						break;

					case "omitnorms":
						bool flag4;
						bool.TryParse(pair.Value, out flag4);
						OmitNorms = flag4;
						break;

					case "multivalued":
						bool flag5;
						bool.TryParse(pair.Value, out flag5);
						MultiValued = flag5;
						break;

					case "indexed":
						bool flag6;
						bool.TryParse(pair.Value, out flag6);
						Indexed = flag6;
						break;

					case "stored":
						bool flag7;
						bool.TryParse(pair.Value, out flag7);
						Stored = flag7;
						break;

					case "required":
						bool flag8;
						bool.TryParse(pair.Value, out flag8);
						Required = flag8;
						break;

					case "default":
						DefaultValue = pair.Value;
						break;

					case "fieldnameformat":
						FieldNameFormat = pair.Value;
						break;

					case "boost":
						Boost = ParseBoost(pair.Value);
						break;

					case "schematype":
						SchemaType = pair.Value;
						break;

					case "type":
						SystemType = SetType(pair.Value);
						break;

					case "cultureformat":
						CultureFormat = pair.Value;
						break;
				}
			}
		}

		public bool CanHandleType(Type valueType)
		{
			return valueType == SystemType;
		}

		public string FormatFieldName(string fieldName, ISearchIndexSchema schema, string cultureCode)
		{
			fieldName = fieldName.Replace(" ", "_").ToLowerInvariant();
			var cultureFormat = string.Empty;
			var defaultLanguage = Settings.DefaultLanguage;
			
			var parsedCulture = !string.IsNullOrEmpty(cultureCode) ? cultureCode : defaultLanguage;
			if (parsedCulture == "iv")
			{
				parsedCulture = defaultLanguage;
			}
			
			if (!string.IsNullOrEmpty(cultureCode) && !string.IsNullOrEmpty(CultureFormat) && !defaultLanguage.StartsWith(parsedCulture))
			{
				cultureFormat = CultureFormat;
			}

			var languageFieldName = fieldName + string.Format(cultureFormat, string.Empty, parsedCulture);
			return schema.AllFieldNames.Contains(languageFieldName)
				       ? languageFieldName
				       : string.Format(FieldNameFormat + cultureFormat, fieldName, parsedCulture);
		}

		private float ParseBoost(string value)
		{
			float num;
			return !float.TryParse(value, out num) ? 1 : num;
		}

		private Type SetType(string type)
		{
			return (Type.GetType(type) ?? Type.GetType("System.String"));
		}
	}
}

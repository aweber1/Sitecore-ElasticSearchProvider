using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Sitecore.Configuration;
using Sitecore.Diagnostics;

namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchFieldNameTranslator : AbstractFieldNameTranslator
	{
		// Fields
		private readonly ElasticSearchIndex _index;
		private string _currentCultureCode;
		private readonly string _defaultCultureCode;
		private readonly ElasticSearchFieldMap _fieldMap;

		// Methods
		public ElasticSearchFieldNameTranslator(ElasticSearchIndex index)
		{
			Assert.ArgumentNotNull(index, "index");
			_index = index;
			_fieldMap = _index.Configuration.FieldMap as ElasticSearchFieldMap;
			_defaultCultureCode = Settings.DefaultLanguage;
			_currentCultureCode = _defaultCultureCode;
		}

		public override string GetIndexFieldName(MemberInfo member)
		{
			var memberAttribute = GetMemberAttribute(member);
			if (memberAttribute != null)
			{
				return GetIndexFieldName(memberAttribute.GetIndexFieldName(member.Name));
			}
			return GetIndexFieldName(member.Name);
		}

		public override string GetIndexFieldName(string fieldName)
		{
			fieldName = fieldName.Replace(" ", "_");
			return fieldName.ToLowerInvariant();
		}

		public override IEnumerable<string> GetTypeFieldNames(string fieldName)
		{
			yield return fieldName;
			if (!fieldName.StartsWith("_"))
			{
				yield return fieldName.Replace("_", " ").Trim();
			}
		}

		public override Dictionary<string, List<string>> MapDocumentFieldsToType(Type type, IEnumerable<string> documentFieldNames)
		{
			var dictionary = documentFieldNames.ToDictionary(f => f, f => GetTypeFieldNames(f).ToList());

			foreach (var info in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				var memberAttribute = GetMemberAttribute(info);
				if (memberAttribute == null) 
					continue;

				var indexFieldName = GetIndexFieldName(memberAttribute.GetIndexFieldName(info.Name));
				if (dictionary.ContainsKey(indexFieldName))
				{
					dictionary[indexFieldName].Add(memberAttribute.GetTypeFieldName(info.Name));
				}
			}
			return dictionary;
		}

		//TODO: is this method necessary for ElasticSearch?
		public void AddCultureContext(CultureInfo culture)
		{
			if (culture != null)
			{
				_currentCultureCode = culture.TwoLetterISOLanguageName;
			}
			if (_currentCultureCode == "iv")
			{
				_currentCultureCode = _defaultCultureCode;
			}
		}

		//TODO: is this method necessary for ElasticSearch?
		public bool HasCulture(string fieldName)
		{
			//return Enumerable.Any<string>(from culture in this.schema.AllCultures
			//							  where (bool)(fieldName.get_Length() > culture.get_Length())
			//							  select culture, new Func<string, bool>(fieldName.EndsWith));
			throw new NotImplementedException();
		}

		//TODO: is this method necessary for ElasticSearch?
		public string StripKnownExtensions(IEnumerable<string> fields)
		{
			var set = new HashSet<string>();
			foreach (var str in fields)
			{
				set.Add(StripKnownExtensions(str));
			}
			return string.Join(",", set);
		}

		//TODO: is this method necessary for ElasticSearch?
		public string StripKnownExtensions(string fieldName)
		{
			fieldName = StripKnownCultures(fieldName);
			foreach (var configuration in _fieldMap.AvailableTypes)
			{
				if (fieldName.StartsWith("_") && !fieldName.StartsWith("__")) 
					continue;

				var str = configuration.FieldNameFormat.Replace("{0}", string.Empty);
				if (fieldName.EndsWith(str))
				{
					fieldName = fieldName.Substring(0, fieldName.Length - str.Length);
				}
				if (fieldName.StartsWith(str))
				{
					fieldName = fieldName.Substring(str.Length, fieldName.Length);
				}
			}
			return fieldName;
		}

		//TODO: is this method necessary for ElasticSearch?
		public string StripKnownCultures(string fieldName)
		{
			//foreach (string str in from culture in this.schema.AllCultures
			//					   where (bool)(fieldName.Length > culture.Length)
			//					   where fieldName.EndsWith(culture)
			//					   select culture)
			//{
			//	fieldName = fieldName.Substring(0, fieldName.Length - str.Length);
			//}
			return fieldName;
		}


	}
}

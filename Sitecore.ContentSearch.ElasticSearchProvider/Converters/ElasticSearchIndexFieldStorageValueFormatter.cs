using System;
using Sitecore.ContentSearch.Converters;
using Sitecore.Data;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Converters
{
	public class ElasticSearchIndexFieldStorageValueFormatter : IndexFieldStorageValueFormatter
	{
		public ElasticSearchIndexFieldStorageValueFormatter()
		{
			AddConverter(typeof(Guid), new IndexFieldGuidValueConverter());
			AddConverter(typeof(ID), new IndexFields.IndexFieldIDValueConverter());
			AddConverter(typeof(ShortID), new IndexFieldShortIDValueConverter());
			AddConverter(typeof(DateTime), new IndexFieldDateTimeValueConverter());
			AddConverter(typeof(SitecoreItemId), new IndexFieldSitecoreItemIDValueConvertor(new IndexFields.IndexFieldIDValueConverter()));
			AddConverter(typeof(SitecoreItemUniqueId), new IndexFieldSitecoreItemUniqueIDValueConvertor(new IndexFieldItemUriValueConvertor()));
			AddConverter(typeof(ItemUri), new IndexFieldItemUriValueConvertor());
			EnumerableConverter = new IndexFieldEnumerableConverter(this);
		}

		public override object FormatValueForIndexStorage(object value)
		{
			if (value == null)
				return null;

			var typeConverter = Converters.GetTypeConverter(value.GetType());
			return typeConverter != null ? typeConverter.ConvertToString(value) : value;
		}

		//code below is same as original, just here for debugging

		//public override object ReadFromIndexStorage(object indexValue, Type destinationType)
		//{
		//	if (indexValue == null)
		//	{
		//		return null;
		//	}
		//	if (destinationType == null)
		//	{
		//		throw new ArgumentNullException("destinationType");
		//	}
		//	if (destinationType.IsInstanceOfType(indexValue))
		//	{
		//		return indexValue;
		//	}
		//	try
		//	{
		//		var typeConverter = Converters.GetTypeConverter(destinationType);
		//		if (typeConverter != null)
		//		{
		//			return typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, indexValue);
		//		}
		//		if (EnumerableConverter != null && EnumerableConverter.CanConvertTo(destinationType))
		//		{
		//			var valueType = indexValue.GetType();
		//			if (indexValue is IEnumerable && valueType != typeof(string))
		//			{
		//				var convertedValue = EnumerableConverter.ConvertTo(null, CultureInfo.InvariantCulture, indexValue, destinationType);
		//				return convertedValue;
		//			}
		//			if (destinationType != typeof(string) && (string)indexValue != string.Empty)
		//			{
		//				return EnumerableConverter.ConvertTo(null, CultureInfo.InvariantCulture, new[] { indexValue }, destinationType);
		//			}
		//		}
		//		if (typeof(ValueType).IsAssignableFrom(destinationType) || (string) indexValue != string.Empty)
		//		{
		//			return System.Convert.ChangeType(indexValue, destinationType);
		//		}
		//	}
		//	catch (InvalidCastException exception)
		//	{
		//		throw new InvalidCastException(string.Format("Could not convert value of type {0} to destination type {1}: {2}", indexValue.GetType().FullName, destinationType.FullName, exception.Message), exception);
		//	}
		//	return null;
		//}
	}
}

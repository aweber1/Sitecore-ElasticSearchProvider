using Newtonsoft.Json.Linq;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Converters.IndexFields
{
	//this class is necessary so we can convert from JValue object to Sitecore.Data.ID object
	public class IndexFieldIDValueConverter : ContentSearch.Converters.IndexFieldIDValueConverter
	{
		public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			var castValue = value as JValue;
			if (castValue != null)
				value = castValue.Value;

			return base.ConvertFrom(context, culture, value);
		}
	}
}

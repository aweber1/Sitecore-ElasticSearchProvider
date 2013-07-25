namespace Sitecore.ContentSearch.ElasticSearchProvider
{
	public class ElasticSearchBoostedField
	{
		// Methods
		public ElasticSearchBoostedField(object value, float? boost)
		{
			FieldValue = value;
			FieldBoost = boost;
		}

		// Properties
		public float? FieldBoost { get; private set; }

		public object FieldValue { get; private set; }
	}


}

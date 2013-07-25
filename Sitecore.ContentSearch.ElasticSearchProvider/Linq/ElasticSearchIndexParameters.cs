using System;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Parsing;

namespace Sitecore.ContentSearch.ElasticSearchProvider.Linq
{
	public class ElasticSearchIndexParameters
	{
		// Methods
		public ElasticSearchIndexParameters(IIndexValueFormatter valueFormatter, IFieldQueryTranslatorMap<IFieldQueryTranslator> fieldQueryTranslators, FieldNameTranslator fieldNameTranslator, IExecutionContext executionContext)
		{
			if (valueFormatter == null)
			{
				throw new ArgumentNullException("valueFormatter");
			}
			if (fieldQueryTranslators == null)
			{
				throw new ArgumentNullException("fieldQueryTranslators");
			}
			if (fieldNameTranslator == null)
			{
				throw new ArgumentNullException("fieldNameTranslator");
			}
			ValueFormatter = valueFormatter;
			FieldNameTranslator = fieldNameTranslator;
			FieldQueryTranslators = fieldQueryTranslators;
			ExecutionContext = executionContext;
		}

		// Properties
		public IExecutionContext ExecutionContext { get; protected set; }

		public FieldNameTranslator FieldNameTranslator { get; protected set; }

		public IFieldQueryTranslatorMap<IFieldQueryTranslator> FieldQueryTranslators { get; protected set; }

		public IIndexValueFormatter ValueFormatter { get; protected set; }

	}
}

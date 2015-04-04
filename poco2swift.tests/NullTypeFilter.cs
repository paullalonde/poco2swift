using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using poco2swift.probe;

namespace poco2swift.tests
{
	class NullTypeFilter : ITypeFilter
	{
		public bool IsGoodType(TypeProxy type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return true;
		}

		public string GetTypeName(TypeProxy type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (type.IsGenericType)
			{
				var name = type.Name;
				int backquote = name.IndexOf('`');

				if (backquote >= 0)
					name = name.Substring(0, backquote);

				return name;
			}

			return type.Name;
		}

		public bool IsGoodProperty(PropertyProxy property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			return true;
		}

		public string GetPropertyName(PropertyProxy property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			return property.Name;
		}

		public IEnumerable<PropertyProxy> SortProperties(IEnumerable<PropertyProxy> properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			return properties;
		}

		public bool IsGoodEnumValue(EnumValueProxy value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return true;
		}

		public string GetEnumName(EnumValueProxy value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return value.ToString();
		}

		public object GetUnderlyingEnumValue(EnumValueProxy value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return value.GetUnderlyingValue();
		}
	}
}

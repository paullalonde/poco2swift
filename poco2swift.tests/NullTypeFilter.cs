using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.tests
{
	class NullTypeFilter : ITypeFilter
	{
		public bool IsGoodType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return true;
		}

		public string GetTypeName(Type type)
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

		public bool IsGoodProperty(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			return true;
		}

		public string GetPropertyName(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			return property.Name;
		}

		public IEnumerable<PropertyInfo> SortProperties(IEnumerable<PropertyInfo> properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			return properties;
		}

		public bool IsGoodEnumValue(Enum value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return true;
		}

		public string GetEnumName(Enum value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return value.ToString();
		}

		public object GetUnderlyingEnumValue(Enum value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			var enumType = value.GetType();
			var underlyingType = Enum.GetUnderlyingType(enumType);

			return Convert.ChangeType(value, underlyingType);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using poco2swift.probe;

namespace poco2swift.probe
{
	public interface ITypeFilter
	{
		bool IsGoodType(TypeProxy type);
		string GetTypeName(TypeProxy type);
		bool IsGoodProperty(PropertyProxy property);
		string GetPropertyName(PropertyProxy property);
		IEnumerable<PropertyProxy> SortProperties(IEnumerable<PropertyProxy> properties);
		bool IsGoodEnumValue(EnumValueProxy value);
		string GetEnumName(EnumValueProxy value);
		object GetUnderlyingEnumValue(EnumValueProxy value);
	}
}

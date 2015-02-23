using System;
using System.Collections.Generic;
using System.Reflection;

namespace poco2swift
{
	public interface ITypeFilter
	{
		bool IsGoodType(Type type);
		string GetTypeName(Type type);
		bool IsGoodProperty(PropertyInfo property);
		string GetPropertyName(PropertyInfo property);
		IEnumerable<PropertyInfo> SortProperties(IEnumerable<PropertyInfo> properties);
		bool IsGoodEnumValue(Enum value);
		string GetEnumName(Enum value);
		object GetUnderlyingEnumValue(Enum value);
	}
}

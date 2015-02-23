using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace poco2swift
{
	class DataContractFilter : ITypeFilter, IComparer<PropertyInfo>
	{
		public DataContractFilter(Poco2SwiftType configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");

			_configuration = configuration;

			CacheConfiguration();
		}

		#region ITypeFilter implementation

		public bool IsGoodType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (IsSkippedType(type))
				return false;

			if (type.IsEnum)
			{
				return IsGoodEnumType(type);
			}
			else if (type.IsClass || type.IsValueType)
			{
				return IsGoodClassType(type);
			}

			return false;
		}

		public string GetTypeName(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			var contract = GetContract(type);

			if (contract != null)
			{
				if (!String.IsNullOrEmpty(contract.Name))
					return contract.Name;
			}

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

			var propertyType = ConfigurationForProperty(property);

			if ((propertyType != null) && propertyType.skipSpecified && propertyType.skip)
				return false;

			//if (!IsGoodType(property.DeclaringType))
			//	return false;

			var contract = GetContract(property);

			if (contract != null)
				return true;

			return false;
		}

		public string GetPropertyName(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			//if (IsGoodType(property.DeclaringType))
			{
				var propertyType = ConfigurationForProperty(property);

				if ((propertyType != null) && !String.IsNullOrEmpty(propertyType.swiftname))
					return propertyType.swiftname;

				var contract = GetContract(property);

				if (contract != null)
				{
					if (!String.IsNullOrEmpty(contract.Name))
						return contract.Name;
				}
			}

			return property.Name;
		}

		public IEnumerable<PropertyInfo> SortProperties(IEnumerable<PropertyInfo> properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			return properties.OrderBy(pi => pi, this);
		}

		public bool IsGoodEnumValue(Enum value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			var valueType = ConfigurationForEnumValue(value);

			if ((valueType != null) && valueType.skipSpecified && valueType.skip)
				return false;

			var enumType = value.GetType();
			var contract = GetContract(enumType);

			if (contract == null)
				return true;

			var enumContract = GetContract(enumType, value);

			return (enumContract != null);
		}

		public string GetEnumName(Enum value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			var valueType = ConfigurationForEnumValue(value);

			if ((valueType != null) && !String.IsNullOrEmpty(valueType.swiftname))
				return valueType.swiftname;

			return value.ToString();
		}

		public object GetUnderlyingEnumValue(Enum value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			var enumType = value.GetType();
			var contract = GetContract(enumType);
			object sourceValue = value;

			if (contract != null)
			{
				var enumContract = GetContract(enumType, value);

				if (enumContract != null)
				{
					if (!String.IsNullOrEmpty(enumContract.Value))
					{
						sourceValue = enumContract.Value;
					}
				}
			}

			var underlyingType = Enum.GetUnderlyingType(enumType);

			return Convert.ChangeType(sourceValue, underlyingType);
		}

		#endregion

		#region IComparer<PropertyInfo> implementation

		int IComparer<PropertyInfo>.Compare(PropertyInfo x, PropertyInfo y)
		{
			var order1 = GetOrder(x);
			var order2 = GetOrder(y);

			if ((order1 >= 0) && (order2 >= 0))
				return order1.CompareTo(order2);
			else if (order1 >= 0)
				return 1;
			else if (order2 >= 0)
				return -1;
			else
				return GetName(x).CompareTo(GetName(y));
		}

		#endregion

		private bool IsGoodClassType(Type classType)
		{
			return (GetContract(classType) != null);
		}

		private bool IsGoodEnumType(Type enumType)
		{
			return true;
		}

		private DataContractAttribute GetContract(Type type)
		{
			if (type.IsClass || type.IsValueType || type.IsEnum)
			{
				DataContractAttribute contract = null;

				if (!_typeContracts.TryGetValue(type, out contract))
				{
					contract = type.GetCustomAttribute(typeof(DataContractAttribute)) as DataContractAttribute;

					_typeContracts.Add(type, contract);
				}
				
				return contract;
			}

			return null;
		}

		private DataMemberAttribute GetContract(PropertyInfo property)
		{
			DataMemberAttribute contract = null;

			if (!_propertyContracts.TryGetValue(property, out contract))
			{
				contract = property.GetCustomAttribute(typeof(DataMemberAttribute)) as DataMemberAttribute;

				_propertyContracts.Add(property, contract);
			}

			return contract;
		}

		private EnumMemberAttribute GetContract(Type enumType, Enum value)
		{
			EnumMemberAttribute contract = null;

			if (!_enumContracts.TryGetValue(value, out contract))
			{
				var member = enumType.GetMember(value.ToString())[0];

				contract = member.GetCustomAttribute(typeof(EnumMemberAttribute)) as EnumMemberAttribute;

				_enumContracts.Add(value, contract);
			}

			return contract;
		}

		private int GetOrder(PropertyInfo property)
		{
			var contract = GetContract(property);

			return (contract != null) ? contract.Order : -1;
		}

		private string GetName(PropertyInfo property)
		{
			var contract = GetContract(property);

			if ((contract != null) && !String.IsNullOrEmpty(contract.Name))
				return contract.Name;
			else
				return property.Name;
		}

		private PropertyType ConfigurationForProperty(PropertyInfo property)
		{
			var ct = ConfigurationForClassType(property.DeclaringType);

			if ((ct == null) || (ct.property == null))
				return null;

			foreach (var propertyType in ct.property)
			{
				if (propertyType.name == property.Name)
					return propertyType;
			}

			return null;
		}

		private EnumValueType ConfigurationForEnumValue(Enum value)
		{
			var ct = ConfigurationForEnumType(value.GetType());

			if ((ct == null) || (ct.value == null))
				return null;

			foreach (var valueType in ct.value)
			{
				if (valueType.name == value.ToString())
					return valueType;
			}

			return null;
		}

		private ClassType ConfigurationForClassType(Type classType)
		{
			var tt = ConfigurationForType(classType);

			return (tt != null) ? tt as ClassType : null;
		}

		private EnumType ConfigurationForEnumType(Type classType)
		{
			var tt = ConfigurationForType(classType);

			return (tt != null) ? tt as EnumType : null;
		}

		private TypeType ConfigurationForType(Type type)
		{
			TypeType tt;

			if (_typesByFullname.TryGetValue(type.FullName, out tt))
				return tt;

			if (_typesByName.TryGetValue(type.Name, out tt))
				return tt;

			return null;
		}

		private bool IsSkippedType(Type type)
		{
			var fullname = type.FullName;

			foreach (var skipFullname in _skipFullnames)
			{
				if (skipFullname == fullname)
					return true;
			}

			foreach (var skipRegex in _skipRegexes)
			{
				if (skipRegex.IsMatch(fullname))
					return true;
			}

			return false;
		}

		private void CacheConfiguration()
		{
			var allTypes = _configuration.classes.Cast<TypeType>().Union(_configuration.enumerations);

			_typesByName = allTypes.Where(tt => !String.IsNullOrEmpty(tt.name)).ToDictionary(tt => tt.name);
			_typesByFullname = allTypes.Where(tt => !String.IsNullOrEmpty(tt.fullname)).ToDictionary(tt => tt.fullname);

			_skipFullnames = new HashSet<string>(from st in _configuration.skiptypes where !String.IsNullOrEmpty(st.fullname) select st.fullname);
			_skipRegexes = from st in _configuration.skiptypes where !String.IsNullOrEmpty(st.match) select new Regex(st.match, RegexOptions.ECMAScript);
		}

		private readonly Poco2SwiftType _configuration;
		private readonly IDictionary<Type, DataContractAttribute> _typeContracts = new Dictionary<Type, DataContractAttribute>();
		private readonly IDictionary<PropertyInfo, DataMemberAttribute> _propertyContracts = new Dictionary<PropertyInfo, DataMemberAttribute>();
		private readonly IDictionary<Enum, EnumMemberAttribute> _enumContracts = new Dictionary<Enum, EnumMemberAttribute>();
		private ISet<string> _skipFullnames;
		private IEnumerable<Regex> _skipRegexes;
		private IDictionary<string, TypeType> _typesByName;
		private IDictionary<string, TypeType> _typesByFullname;
	}
}

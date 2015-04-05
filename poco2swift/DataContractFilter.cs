using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using poco2swift.probe;

namespace poco2swift
{
	class DataContractFilter : ITypeFilter, IComparer<PropertyProxy>
	{
		public DataContractFilter(Poco2SwiftType configuration)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");

			_configuration = configuration;

			CacheConfiguration();
		}

		#region ITypeFilter implementation

		public bool IsGoodType(TypeProxy type)
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

		public string GetTypeName(TypeProxy type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			var contract = GetContract(type);

			if (contract != null)
			{
				if (!String.IsNullOrEmpty(contract.Name))
					return contract.Name;
			}

			var tt = ConfigurationForType(type);

			if ((tt != null) && (tt.EffectiveSwiftName != null))
				return tt.EffectiveSwiftName;

			return TypeType.MakeSwiftSafeName(type.Name);
		}

		public bool IsGoodProperty(PropertyProxy property)
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

		public string GetPropertyName(PropertyProxy property)
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

		public IEnumerable<PropertyProxy> SortProperties(IEnumerable<PropertyProxy> properties)
		{
			if (properties == null)
				throw new ArgumentNullException("properties");

			return properties.OrderBy(pi => pi, this);
		}

		public bool IsGoodEnumValue(EnumValueProxy value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			var valueType = ConfigurationForEnumValue(value);

			if ((valueType != null) && valueType.skipSpecified && valueType.skip)
				return false;

			var enumType = value.GetEnumType();
			var contract = GetContract(enumType);

			if (contract == null)
				return true;

			var enumContract = GetContract(value);

			return (enumContract != null);
		}

		public string GetEnumName(EnumValueProxy value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			var valueType = ConfigurationForEnumValue(value);

			if ((valueType != null) && !String.IsNullOrEmpty(valueType.swiftname))
				return valueType.swiftname;

			return value.Name;
		}

		public object GetUnderlyingEnumValue(EnumValueProxy value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			var enumType = value.GetEnumType();
			var contract = GetContract(enumType);
			object underlyingValue = value.GetUnderlyingValue();

			if (contract != null)
			{
				var enumContract = GetContract(value);

				if (enumContract != null)
				{
					if (!String.IsNullOrEmpty(enumContract.Value))
					{
						underlyingValue = value.ConvertToUnderlyingValue(enumContract.Value);
					}
				}
			}

			return underlyingValue;
		}

		#endregion

		#region IComparer<PropertyInfo> implementation

		int IComparer<PropertyProxy>.Compare(PropertyProxy x, PropertyProxy y)
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

		private bool IsGoodClassType(TypeProxy classType)
		{
			return (GetContract(classType) != null);
		}

		private bool IsGoodEnumType(TypeProxy enumType)
		{
			return true;
		}

		private DataContractProxy GetContract(TypeProxy type)
		{
			if (type.IsClass || type.IsValueType || type.IsEnum)
			{
				DataContractProxy contract = null;

				if (!_typeContracts.TryGetValue(type, out contract))
				{
					contract = type.GetDataContract();

					_typeContracts.Add(type, contract);
				}
				
				return contract;
			}

			return null;
		}

		private DataMemberProxy GetContract(PropertyProxy property)
		{
			DataMemberProxy contract = null;

			if (!_propertyContracts.TryGetValue(property, out contract))
			{
				contract = property.GetDataContract();

				_propertyContracts.Add(property, contract);
			}

			return contract;
		}

		private EnumMemberProxy GetContract(EnumValueProxy value)
		{
			EnumMemberProxy contract = null;

			if (!_enumContracts.TryGetValue(value, out contract))
			{
				contract = value.GetDataContract();

				_enumContracts.Add(value, contract);
			}

			return contract;
		}

		private int GetOrder(PropertyProxy property)
		{
			var contract = GetContract(property);

			return (contract != null) ? contract.Order : -1;
		}

		private string GetName(PropertyProxy property)
		{
			var contract = GetContract(property);

			if ((contract != null) && !String.IsNullOrEmpty(contract.Name))
				return contract.Name;
			else
				return property.Name;
		}

		private PropertyType ConfigurationForProperty(PropertyProxy property)
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

		private EnumValueType ConfigurationForEnumValue(EnumValueProxy value)
		{
			var ct = ConfigurationForEnumType(value.GetEnumType());

			if ((ct == null) || (ct.value == null))
				return null;

			foreach (var valueType in ct.value)
			{
				if (valueType.name == value.ToString())
					return valueType;
			}

			return null;
		}

		private ClassType ConfigurationForClassType(TypeProxy classType)
		{
			var tt = ConfigurationForType(classType);

			return (tt != null) ? tt as ClassType : null;
		}

		private EnumType ConfigurationForEnumType(TypeProxy classType)
		{
			var tt = ConfigurationForType(classType);

			return (tt != null) ? tt as EnumType : null;
		}

		private TypeType ConfigurationForType(TypeProxy type)
		{
			TypeType tt;

			if (_typesByFullname.TryGetValue(type.FullName, out tt))
				return tt;

			if (_typesByName.TryGetValue(type.Name, out tt))
				return tt;

			return null;
		}

		private bool IsSkippedType(TypeProxy type)
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

			foreach (var tt in allTypes)
			{
				var name = tt.EffectiveName;

				if (!String.IsNullOrEmpty(name))
				{
					if (!_typesByName.ContainsKey(name))
						_typesByName.Add(name, tt);
					else
						ErrorHandler.Error("Type '{0}' already configured.", name);
				}

				var fullName = tt.EffectiveFullName;

				if (!String.IsNullOrEmpty(fullName))
				{
					if (!_typesByFullname.ContainsKey(fullName))
						_typesByFullname.Add(fullName, tt);
					else
						ErrorHandler.Error("Type '{0}' already configured.", fullName);
				}
			}

			_skipFullnames = new HashSet<string>(from st in _configuration.skiptypes where !String.IsNullOrEmpty(st.fullname) select st.fullname);
			_skipRegexes = from st in _configuration.skiptypes where !String.IsNullOrEmpty(st.match) select new Regex(st.match, RegexOptions.ECMAScript);
		}

		private readonly Poco2SwiftType _configuration;
		private readonly IDictionary<TypeProxy, DataContractProxy> _typeContracts = new Dictionary<TypeProxy, DataContractProxy>();
		private readonly IDictionary<PropertyProxy, DataMemberProxy> _propertyContracts = new Dictionary<PropertyProxy, DataMemberProxy>();
		private readonly IDictionary<EnumValueProxy, EnumMemberProxy> _enumContracts = new Dictionary<EnumValueProxy, EnumMemberProxy>();
		private readonly IDictionary<string, TypeType> _typesByName = new Dictionary<string, TypeType>();
		private readonly IDictionary<string, TypeType> _typesByFullname = new Dictionary<string, TypeType>();
		private ISet<string> _skipFullnames;
		private IEnumerable<Regex> _skipRegexes;
	}
}

using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace poco2swift.probe
{
	public class EnumValueProxy : MarshalByRefObject
	{
		public EnumValueProxy(IProxyUtils utils, Enum value)
		{
			if (utils == null)
				throw new ArgumentNullException("utils");

			_utils = utils;
			_value = value;
		}

		public string Name
		{
			get { return Enum.GetName(_value.GetType(), _value); }
		}

		public MemberProxy GetMember()
		{
			var valueName = Enum.GetName(_value.GetType(), _value);
			var member = _value.GetType().GetMember(valueName)[0];

			return new MemberProxy(_utils, member);
		}

		public TypeProxy GetEnumType()
		{
			return MakeTypeProxy(_value.GetType());
		}

		public EnumMemberProxy GetDataContract()
		{
			var enumType = _value.GetType();
			var member = enumType.GetMember(_value.ToString())[0];

			var contract = member.GetCustomAttribute(typeof(EnumMemberAttribute)) as EnumMemberAttribute;

			if (contract != null)
			{
				return new EnumMemberProxy
				{
					Value = contract.Value,
				};
			}

			return null;
		}

		public object GetUnderlyingValue()
		{
			return ConvertToUnderlyingValue(_value);
		}

		public object ConvertToUnderlyingValue(object value)
		{
			var enumType = _value.GetType();
			var underlyingType = enumType.GetEnumUnderlyingType();

			return Convert.ChangeType(value, underlyingType);

		}

		private TypeProxy MakeTypeProxy(Type type)
		{
			return _utils.MakeTypeProxy(type);
		}

		private readonly IProxyUtils _utils;
		private readonly Enum _value;
	}
}

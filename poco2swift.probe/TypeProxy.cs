using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.probe
{
	public class TypeProxy : MarshalByRefObject
	{
		public TypeProxy(IProxyUtils utils, Type type)
		{
			if (utils == null)
				throw new ArgumentNullException("utils");

			if (type == null)
				throw new ArgumentNullException("type");

			_utils = utils;
			_type = type;
		}

		#region Type forwarding

		public AssemblyProxy Assembly { get { return _utils.MakeAssemblyProxyForType(_type); } }
		public TypeProxy BaseType { get { return MakeTypeProxy(_type.BaseType); } }
		public string FullName { get { return _type.FullName; } }
		public bool IsArray { get { return _type.IsArray; } }
		public bool IsEnum { get { return _type.IsEnum; } }
		public bool IsClass { get { return _type.IsClass; } }
		public bool IsGenericParameter { get { return _type.IsGenericParameter; } }
		public bool IsGenericType { get { return _type.IsGenericType; } }
		public bool IsGenericTypeDefinition { get { return _type.IsGenericTypeDefinition; } }
		public bool IsInterface { get { return _type.IsInterface; } }
		public bool IsValueType { get { return _type.IsValueType; } }
		public string Name { get { return _type.Name; } }

		public TypeProxy GetElementType()
		{
			return MakeTypeProxy(_type.GetElementType());
		}

		public TypeProxy GetGenericTypeDefinition()
		{
			return MakeTypeProxy(_type.GetGenericTypeDefinition());
		}

		public IList<TypeProxy> GetGenericArguments()
		{
			return (from argType in _type.GetGenericArguments() select MakeTypeProxy(argType)).ToList();
		}

		public IList<TypeProxy> GetGenericParameterConstraints()
		{
			return (from constraintType in _type.GetGenericParameterConstraints() select MakeTypeProxy(constraintType)).ToList();
		}

		public IList<PropertyProxy> GetProperties(BindingFlags bindingFlags)
		{
			return (from propType in _type.GetProperties(bindingFlags) select new PropertyProxy(_utils, propType)).ToList();
		}

		public bool IsAssignableFrom(TypeProxy type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return _type.IsAssignableFrom(type._type);
		}

		public TypeProxy MakeGenericType(params TypeProxy[] typeArgs)
		{
			return MakeTypeProxy(_type.MakeGenericType(typeArgs.Select(t => t._type).ToArray()));
		}

		#endregion

		#region Enums

		public TypeProxy GetEnumUnderlyingType()
		{
			if (!_type.IsEnum)
				throw new InvalidOperationException("Type not a Enum.");

			return MakeTypeProxy(Enum.GetUnderlyingType(_type));
		}

		public IList<EnumValueProxy> GetEnumValues()
		{
			if (!_type.IsEnum)
				throw new InvalidOperationException("Type not a Enum.");

			return (from val in Enum.GetValues(_type).Cast<Enum>() select new EnumValueProxy(_utils, val)).ToList();
		}

		#endregion

		#region MarshalByRefObject overrides

		public override object InitializeLifetimeService()
		{
			return null;
		}

		#endregion

		public DataContractProxy GetDataContract()
		{
			var contract = _type.GetCustomAttribute(typeof(DataContractAttribute)) as DataContractAttribute;

			if (contract != null)
			{
				return new DataContractProxy
				{
					IsReference = contract.IsReference,
					Name = contract.Name,
					Namespace = contract.Namespace,
				};
			}

			return null;
		}

		private TypeProxy MakeTypeProxy(Type type)
		{
			return _utils.MakeTypeProxy(type);
		}

		public bool Equals(TypeProxy other)
		{
			if (other == null)
				return false;
			else if (Object.ReferenceEquals(this, other))
				return true;
			else
				return _type.Equals(other._type);
		}

		#region Object overrides

		public override bool Equals(object obj)
		{
			return Equals(obj as TypeProxy);
		}

		public override int GetHashCode()
		{
			return _type.GetHashCode();
		}

		public override string ToString()
		{
			return _type.ToString();
		}

		#endregion

		public static bool operator ==(TypeProxy p1, TypeProxy p2)
		{
			if (!Object.ReferenceEquals(p1, null))
				return p1.Equals(p2);
			else if (!Object.ReferenceEquals(p2, null))
				return p2.Equals(p1);
			else
				return true;
		}

		public static bool operator !=(TypeProxy p1, TypeProxy p2)
		{
			return !(p1 == p2);
		}

		private readonly IProxyUtils _utils;
		private readonly Type _type;
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.probe
{
	public class PropertyProxy : MarshalByRefObject
	{
		public PropertyProxy(IProxyUtils utils, PropertyInfo property)
		{
			if (utils == null)
				throw new ArgumentNullException("utils");

			if (property == null)
				throw new ArgumentNullException("property");

			_utils = utils;
			_property = property;
		}

		#region PropertyInfo forwarding

		public TypeProxy DeclaringType { get { return MakeTypeProxy(_property.DeclaringType); } }
		public string Name { get { return _property.Name; } }
		public TypeProxy PropertyType { get { return MakeTypeProxy(_property.PropertyType); } }

		#endregion

		#region MarshalByRefObject overrides

		public override object InitializeLifetimeService()
		{
			return null;
		}

		#endregion

		public DataMemberProxy GetDataContract()
		{
			var contract = _property.GetCustomAttribute(typeof(DataMemberAttribute)) as DataMemberAttribute;

			if (contract != null)
			{
				return new DataMemberProxy
				{
					EmitDefaultValue = contract.EmitDefaultValue,
					IsRequired = contract.IsRequired,
					Name = contract.Name,
					Order = contract.Order,
				};
			}

			return null;
		}

		#region Object overrides

		public override string ToString()
		{
			return _property.ToString();
		}

		#endregion

		private TypeProxy MakeTypeProxy(Type type)
		{
			return _utils.MakeTypeProxy(type);
		}

		private readonly IProxyUtils _utils;
		private readonly PropertyInfo _property;
	}
}

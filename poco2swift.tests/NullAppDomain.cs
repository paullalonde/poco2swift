using System;
using System.Collections.Generic;
using poco2swift.probe;

namespace poco2swift.tests
{
	class NullAppDomain : IAppDomainProxy
	{
		public IProxyUtils Utils { get; set; }

		#region IAppDomainProxy implementation

		public TypeProxy ObjectType
		{
			get { return new TypeProxy(this.Utils, typeof(object)); }
		}

		public TypeProxy GetDomainType(string typeName)
		{
			var type = Type.GetType(typeName);

			if (type != null)
				return new TypeProxy(this.Utils, type);
			else
				return null;
		}

		public TypeProxy MakeGenericNullableType(TypeProxy valueTypeProxy)
		{
			return MakeGenericType(typeof(Nullable<>), valueTypeProxy);
		}

		public TypeProxy MakeGenericEnumerableType(TypeProxy elementTypeProxy)
		{
			return MakeGenericType(typeof(IEnumerable<>), elementTypeProxy);
		}

		public TypeProxy MakeGenericDictionaryType(TypeProxy keyTypeProxy, TypeProxy valueTypeProxy)
		{
			return MakeGenericType(typeof(IDictionary<,>), keyTypeProxy, valueTypeProxy);
		}

		#endregion

		private TypeProxy MakeGenericType(Type templateType, TypeProxy argType)
		{
			if (argType == null)
				throw new ArgumentNullException("argType");

			var templateTypeProxy = new TypeProxy(this.Utils, templateType);

			return templateTypeProxy.MakeGenericType(argType);
		}

		private TypeProxy MakeGenericType(Type templateType, TypeProxy argType0, TypeProxy argType1)
		{
			if (argType0 == null)
				throw new ArgumentNullException("argType0");

			if (argType1 == null)
				throw new ArgumentNullException("argType1");

			var templateTypeProxy = new TypeProxy(this.Utils, templateType);

			return templateTypeProxy.MakeGenericType(argType0, argType1);
		}
	}
}

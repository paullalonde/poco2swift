using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using poco2swift.probe;

namespace poco2swift.tests
{
	class NullProxyUtils : IProxyUtils
	{
		#region IProxyUtils implementation

		public IProxyCallback Callback { get; set; }

		public TypeProxy MakeTypeProxy(Type type)
		{
			return new TypeProxy(this, type);
		}

		public AssemblyProxy MakeAssemblyProxyForType(Type type)
		{
			return new AssemblyProxy(this, type.Assembly);
		}

		#endregion
	}
}

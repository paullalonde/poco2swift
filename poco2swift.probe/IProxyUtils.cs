using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.probe
{
	public interface IProxyUtils
	{
		IProxyCallback Callback { get; }
		TypeProxy MakeTypeProxy(Type type);
		AssemblyProxy MakeAssemblyProxyForType(Type type);
	}
}

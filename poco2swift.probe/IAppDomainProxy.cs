using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.probe
{
	public interface IAppDomainProxy
	{
		TypeProxy ObjectType { get; }

		TypeProxy GetDomainType(string typeName);

		TypeProxy MakeGenericNullableType(TypeProxy valueTypeProxy);
		TypeProxy MakeGenericEnumerableType(TypeProxy elementTypeProxy);
		TypeProxy MakeGenericDictionaryType(TypeProxy keyTypeProxy, TypeProxy valueTypeProxy);
	}
}

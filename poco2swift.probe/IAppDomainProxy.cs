using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.probe
{
	/// <summary>
	/// Interface to the type probing functionality running the remote AppDomain.
	/// </summary>
	public interface IAppDomainProxy
	{
		/// <summary>
		/// Gets the type proxy for the CLR type <see cref="object"/>.
		/// </summary>
		TypeProxy ObjectType { get; }

		/// <summary>
		/// Gets the type proxy for the CLR type <see cref="ValueType"/>.
		/// </summary>
		TypeProxy ValueType { get; }

		TypeProxy GetDomainType(string typeName);

		TypeProxy MakeGenericNullableType(TypeProxy valueTypeProxy);
		TypeProxy MakeGenericEnumerableType(TypeProxy elementTypeProxy);
		TypeProxy MakeGenericSetType(TypeProxy elementTypeProxy);
		TypeProxy MakeGenericDictionaryType(TypeProxy keyTypeProxy, TypeProxy valueTypeProxy);
	}
}

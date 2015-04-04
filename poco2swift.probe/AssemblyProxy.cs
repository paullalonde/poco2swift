using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace poco2swift.probe
{
	public class AssemblyProxy : MarshalByRefObject
    {
		public AssemblyProxy(IProxyUtils utils, Assembly assembly)
		{
			if (utils == null)
				throw new ArgumentNullException("utils");

			if (assembly == null)
				throw new ArgumentNullException("assembly");

			_utils = utils;
			_assembly = assembly;
		}

		//internal AssemblyProxy(IProxyCallback callback, string path)
		//{
		//	if (callback == null)
		//		throw new ArgumentNullException("callback");

		//	if (String.IsNullOrEmpty(path))
		//		throw new ArgumentNullException("path");

		//	_callback = callback;
		//	_assembly = Assembly.LoadFrom(path);

		//	_assembly.ModuleResolve += HandleModuleResolve;
		//}

		#region Assembly forwarding

		public IEnumerable<TypeProxy> ExportedTypes
		{
			get
			{
				return (from t in _assembly.ExportedTypes select MakeTypeProxy(t)).ToList();
			}
		}

		public string Location { get { return _assembly.Location; } }

		#endregion

		public bool Equals(AssemblyProxy other)
		{
			if (other == null)
				return false;
			else if (Object.ReferenceEquals(this, other))
				return true;
			else
				return _assembly.Equals(other._assembly);
		}

		#region Object overrides

		public override bool Equals(object obj)
		{
			return Equals(obj as AssemblyProxy);
		}

		public override int GetHashCode()
		{
			return _assembly.GetHashCode();
		}

		public override string ToString()
		{
			return _assembly.ToString();
		}

		#endregion

		public static bool operator ==(AssemblyProxy p1, AssemblyProxy p2)
		{
			if (!Object.ReferenceEquals(p1, null))
				return p1.Equals(p2);
			else if (!Object.ReferenceEquals(p2, null))
				return p2.Equals(p1);
			else
				return true;
		}

		public static bool operator !=(AssemblyProxy p1, AssemblyProxy p2)
		{
			return !(p1 == p2);
		}

		internal IProxyCallback Callback
		{
			get { return _utils.Callback; }
		}

		internal TypeProxy MakeTypeProxy(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			TypeProxy proxy;

			if (!_proxies.TryGetValue(type, out proxy))
			{
				proxy = new TypeProxy(_utils, type);

				_proxies.Add(type, proxy);
			}

			return proxy;
		}

		private Module HandleModuleResolve(object sender, ResolveEventArgs e)
		{
			this.Callback.WriteError("Cannot resolve module '{0}' from assembly '{1}'", e.Name);

			return null;
		}

		private readonly IProxyUtils _utils;
		private readonly Assembly _assembly;
		private readonly IDictionary<Type, TypeProxy> _proxies = new Dictionary<Type, TypeProxy>();
    }
}

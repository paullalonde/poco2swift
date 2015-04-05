using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace poco2swift.probe
{
	public class AppDomainProxy : MarshalByRefObject, IAppDomainProxy
	{
		public AppDomainProxy(AppDomain hostDomain)
		{
			if (hostDomain == null)
				throw new ArgumentNullException("hostDomain");

			this.Callback = new ProxyCallback();

			_hostDomain = hostDomain;
			_appDomain = AppDomain.CurrentDomain;
			_utils = new ProxyUtils(this);

			//_appDomain.AssemblyResolve += HandleAssemblyResolve;
			//_appDomain.AssemblyLoad += HandleAssemblyLoad;
			_appDomain.ReflectionOnlyAssemblyResolve += HandleAssemblyResolve;
		}

		#region IAppDomainProxy implementation

		public TypeProxy ObjectType
		{
			get { return MakeTypeProxy(typeof(object)); }
		}

		public TypeProxy ValueType
		{
			get { return MakeTypeProxy(typeof(ValueType)); }
		}

		public TypeProxy GetDomainType(string typeName)
		{
			if (String.IsNullOrEmpty(typeName))
				throw new ArgumentNullException("typeName");

			var type = Type.GetType(typeName);

			if (type != null)
			{
				var assemblyProxy = MakeAssemblyProxy(type.Assembly);

				return assemblyProxy.MakeTypeProxy(type);
			}

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

		public TypeProxy MakeGenericSetType(TypeProxy elementTypeProxy)
		{
			return MakeGenericType(typeof(ISet<>), elementTypeProxy);
		}

		public TypeProxy MakeGenericDictionaryType(TypeProxy keyTypeProxy, TypeProxy valueTypeProxy)
		{
			return MakeGenericType(typeof(IDictionary<,>), keyTypeProxy, valueTypeProxy);
		}

		#endregion

		#region MarshalByRefObject overrides

		public override object InitializeLifetimeService()
		{
			return null;
		}

		#endregion

		private IProxyCallback Callback { get; set; }

		private TypeProxy MakeTypeProxy(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			AssemblyProxy proxy = MakeAssemblyProxy(type.Assembly);

			return proxy.MakeTypeProxy(type);
		}

		private AssemblyProxy MakeAssemblyProxyForType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return MakeAssemblyProxy(type.Assembly);
		}

		public AssemblyProxy LoadAssembly(string path)
		{
			if (String.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");

			path = ResolvePath(path);

			var foundAssembly = (from asm in _assemblies.Keys where asm.Location == path select asm).FirstOrDefault();

			if (foundAssembly != null)
				return _assemblies[foundAssembly];

			AssemblyProxy proxy = null;

			if (File.Exists(path))
			{
				var assembly = Assembly.LoadFrom(path);

				assembly.ModuleResolve += HandleModuleResolve;

				return MakeAssemblyProxy(assembly);
			}
			else
			{
				this.Callback.WriteError("File does not exist : \"{0}\"", path);
			}

			return proxy;
		}

		internal AssemblyProxy MakeAssemblyProxy(Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			AssemblyProxy proxy = null;

			if (!_assemblies.TryGetValue(assembly, out proxy))
			{
				proxy = new AssemblyProxy(this._utils, assembly);

				_assemblies.Add(assembly, proxy);
			}

			return proxy;
		}

		private TypeProxy MakeGenericType(Type templateType, TypeProxy argType)
		{
			if (argType == null)
				throw new ArgumentNullException("argType");

			var templateTypeProxy = MakeTypeProxy(templateType);

			return templateTypeProxy.MakeGenericType(argType);
		}

		private TypeProxy MakeGenericType(Type templateType, TypeProxy argType0, TypeProxy argType1)
		{
			if (argType0 == null)
				throw new ArgumentNullException("argType0");

			if (argType1 == null)
				throw new ArgumentNullException("argType1");

			var templateTypeProxy = MakeTypeProxy(templateType);

			return templateTypeProxy.MakeGenericType(argType0, argType1);
		}

		private Assembly HandleAssemblyResolve(object sender, ResolveEventArgs e)
		{
			var assembly = e.RequestingAssembly;
			var assemblyName = (assembly != null) ? assembly.GetName().Name : null;

			this.Callback.WriteError("Cannot resolve required assembly '{0}' requested from assembly '{1}'", e.Name, assemblyName);

			return null;
		}

		private void HandleAssemblyLoad(object sender, AssemblyLoadEventArgs e)
		{
			this.Callback.WriteError("Loaded assembly '{0}'", e.LoadedAssembly.GetName().Name);

			var assembly = e.LoadedAssembly;

			foreach (var name in assembly.GetReferencedAssemblies())
			{
				this.Callback.WriteError("Referenced assembly '{0}'", name.Name);

			}

			//throw new NotImplementedException();
		}

		private Module HandleModuleResolve(object sender, ResolveEventArgs e)
		{
			this.Callback.WriteError("Cannot resolve module '{0}' from assembly '{1}'", e.Name);

			return null;
		}

		private string ResolvePath(string path)
		{
			if (!Path.IsPathRooted(path))
				path = Path.Combine(_appDomain.BaseDirectory, path);

			path = Path.GetFullPath(path);

			return path;
		}

		private readonly AppDomain _hostDomain;
		private readonly AppDomain _appDomain;
		private readonly IProxyUtils _utils;
		private readonly IDictionary<Assembly, AssemblyProxy> _assemblies = new Dictionary<Assembly, AssemblyProxy>();

		private class ProxyUtils : IProxyUtils
		{
			public ProxyUtils(AppDomainProxy proxy)
			{
				_proxy = proxy;
			}

			#region IProxyUtils implementation

			public IProxyCallback Callback
			{
				get { return _proxy.Callback; }
			}

			public TypeProxy MakeTypeProxy(Type type)
			{
				return _proxy.MakeTypeProxy(type);
			}

			public AssemblyProxy MakeAssemblyProxyForType(Type type)
			{
				return _proxy.MakeAssemblyProxyForType(type);
			}

			#endregion

			private readonly AppDomainProxy _proxy;
		}
	}
}

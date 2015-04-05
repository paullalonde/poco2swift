using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using poco2swift.probe;

namespace poco2swift
{
	class Program
	{
		static void Main(string[] args)
		{
			int exitCode = 0;

			try
			{
				new Program(args).Run();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Message : {0}", ex.Message);
				Console.Error.WriteLine("StackTrace : {0}", ex.StackTrace);

				exitCode = 1;
			}

			Environment.Exit(exitCode);
		}

		private void Run()
		{
			ReadConfiguration();
			CreateTargetDomain();
			LoadAssemblies();

			_typeFilter = new DataContractFilter(_configuration);
			_documentation = new DocumentationCache();
			_translator = new SwiftTranslator(_configuration, _typeFilter, _documentation, _targetDomainProxy);

			ReadTypes();
			TranslateTypes();
			WriteSwiftSource();
		}

		private void CreateTargetDomain()
		{
			_targetDomainSetup = new AppDomainSetup
			{
				ApplicationBase = _basePath,
				ApplicationName = "poco2swift target app",
				DisallowCodeDownload = true,
			};

			_targetDomain = AppDomain.CreateDomain("poco2swift target", null, _targetDomainSetup);

			var assemblyPath = typeof(AppDomainProxy).Assembly.Location;
			var typeName = typeof(AppDomainProxy).FullName;
			var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;
			var args = new object[] { AppDomain.CurrentDomain };
			var culture = CultureInfo.CurrentCulture;

			_targetDomainProxy = (AppDomainProxy)_targetDomain.CreateInstanceFromAndUnwrap(assemblyPath, typeName, false, bindingFlags, null, args, culture, null);
		}

		private void LoadAssemblies()
		{
			foreach (var path in _dllPaths)
			{
				var assembly = _targetDomainProxy.LoadAssembly(path);

				if (assembly != null)
					_assemblies.Add(assembly);
			}
		}

		private void ReadConfiguration()
		{
			Poco2SwiftType configuration = null;

			if (_configPath == null)
			{
				configuration = new Poco2SwiftType();
			}
			else
			{
				XmlSerializer serializer = new XmlSerializer(typeof(Poco2SwiftType));

				using (var textReader = File.OpenText(_configPath))
				{
					using (var xmlReader = XmlReader.Create(textReader))
					{
						configuration = (Poco2SwiftType)serializer.Deserialize(xmlReader);
						
						configuration.PostDeserialize();
					}
				}
			}

			_configuration = configuration;

			foreach (var @enum in configuration.enumerations)
			{
				if (@enum.EffectiveFullName != null)
					@enum.forceinclude = true;

				_configuredEnums.Add(@enum.EffectiveName, @enum);
			}

			foreach (var @class in configuration.classes)
			{
				if (@class.EffectiveFullName != null)
					@class.forceinclude = true;

				_configuredClasses.Add(@class.EffectiveName, @class);
			}
		}

		private void ReadTypes()
		{
			// Start with all public types in the source assembly.

			foreach (var assembly in _assemblies)
			{
				foreach (var type in assembly.ExportedTypes)
				{
					ReadType(type);
				}
			}

			ReadConfigTypes("enum", _configuration.enumerations);
			ReadConfigTypes("class", _configuration.classes);
		}

		private void ReadConfigTypes(string typeName, IEnumerable<TypeType> configTypes)
		{
			foreach (var configType in configTypes)
			{
				ReadConfigType(typeName, configType);
			}
		}

		private void ReadConfigType(string typeName, TypeType configType)
		{
			if (configType.forceinclude)
			{
				if (String.IsNullOrEmpty(configType.fullname))
				{
					ErrorHandler.Fatal(ErrorHandler.MISSING_FULL_NAME, "Included {0} '{1}' is missing the full-name attribute.", typeName, configType.name);
				}

				var type = _targetDomainProxy.GetDomainType(configType.fullname);

				ReadType(type, forceGoodType: true);
			}
		}

		private void ReadType(TypeProxy type, bool forceGoodType = false)
		{
			if (!type.IsEnum && !type.IsClass && !type.IsValueType)
				return;

			if (!forceGoodType && !_typeFilter.IsGoodType(type))
				return;

			var name = type.Name;
			var swiftName = _typeFilter.GetTypeName(type);
			bool ignore = false;

			if (type.IsEnum)
			{
				EnumType configuredEnum;

				if (_configuredEnums.TryGetValue(name, out configuredEnum))
				{
					//ignore = configuredEnum.ignoreSpecified && configuredEnum.ignore;

					//if (!String.IsNullOrEmpty(configuredEnum.rename))
					//	swiftName = configuredEnum.rename;
				}
			}
			else if (type.IsClass || type.IsValueType)
			{
				ClassType configuredClass;

				if (_configuredClasses.TryGetValue(name, out configuredClass))
				{
					//ignore = configuredClass.ignoreSpecified && configuredClass.ignore;

					//if (!String.IsNullOrEmpty(configuredClass.rename))
					//	swiftName = configuredClass.rename;
				}
			}

			if (!ignore /*&& !_swiftNames.ContainsKey(type)*/)
			{
				//if (_swiftTypes.ContainsKey(swiftName))
				//{
				//	var duplicateTypes = new List<Type> { type }.Union(_swiftNames.Where(kvp => kvp.Value == swiftName).Select(kvp => kvp.Key));

				//	Fatal(DUPLICATE_TYPE_NAME,
				//		"Swift name '{0}' is used by .NET types '{1}'.", swiftName, String.Join("', '", duplicateTypes));
				//}

				//_swiftNames.Add(type, swiftName);
				//_swiftTypes.Add(swiftName, null);

				_types.Add(type);
				_translator.CacheSwiftName(type, swiftName);
			}
		}

		private void TranslateTypes()
		{
			foreach (var type in _types)
			{
				_translator.TranslateType(type);
			}
		}

		private void WriteSwiftSource()
		{
			using (var writer = new FileSwiftWriter(_configuration, _swift12, _outputDir))
			{
				foreach (var tuple in _translator.GetCachedSwiftTypes())
				{
					var swiftType = tuple.Item1;
					var type = tuple.Item2;

					if (swiftType.IsExcluded)
						continue;

					writer.Write(type, swiftType);
				}
			}
		}

		private Program(string[] args)
		{
			_selfLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

			ParseArgs(args);
		}

		private void ParseArgs(string[] args)
		{
			string basePath = null;

			for (int argIndex = 0; argIndex < args.Length; ++argIndex)
			{
				string arg = args[argIndex];

				if (arg.Substring(0, 1).IndexOfAny(new char[] { '-', '/' }) == 0)
				{
					switch (arg.Substring(1).ToLowerInvariant())
					{
						case "b":
							if (argIndex < args.Length - 1)
								basePath = args[++argIndex];
							else
								Usage();
							break;

						case "c":
							if (argIndex < args.Length - 1)
								_configPath = args[++argIndex];
							else
								Usage();
							break;

						case "d":
							if (argIndex < args.Length - 1)
								_dllPaths.Add(args[++argIndex]);
							else
								Usage();
							break;

						case "o":
							if (argIndex < args.Length - 1)
								_outputDir = args[++argIndex];
							else
								Usage();
							break;

						case "s":
							if (argIndex < args.Length - 1)
								_swift12 = (args[++argIndex] == "1.2");
							else
								Usage();
							break;

						default:
							Usage();
							break;
					}
				}
			}

			if (String.IsNullOrEmpty(basePath))
				Usage();

			if (!_dllPaths.Any())
				Usage();

			if (String.IsNullOrEmpty(_outputDir))
				Usage();

			_basePath = ResolvePath(basePath);
		}

		private string ResolvePath(string path)
		{
			if (!Path.IsPathRooted(path))
				path = Path.Combine(_selfLocation, path);

			path = Path.GetFullPath(path);

			return path;
		}

		private void Usage()
		{
			Console.Out.WriteLine("poco2swift [-b <path>] [-c <path>] [-d <path>]...  [-o <path>] [-s 1.2]");
			Console.Out.WriteLine("   -b <path>   The base path for loading the source assemblies.");
			Console.Out.WriteLine("   -c <path>   Path to the configuration file.");
			Console.Out.WriteLine("   -d <path>   Path of an assembly to translate, relative to base path. May be specified more than once.");
			Console.Out.WriteLine("   -o <path>   Directory to receive generated Swift source files.");
			Console.Out.WriteLine("   -s 1.2      Emit Swift 1.2");

			Environment.Exit(20);
		}

		private string _basePath = null;
		private IList<string> _dllPaths = new List<string>();
		private string _outputDir = ".";
		private string _configPath;
		private IList<AssemblyProxy> _assemblies = new List<AssemblyProxy>();
		private Poco2SwiftType _configuration;
		private IDictionary<string, EnumType> _configuredEnums = new Dictionary<string, EnumType>();
		private IDictionary<string, ClassType> _configuredClasses = new Dictionary<string, ClassType>();
		private ISet<TypeProxy> _types = new HashSet<TypeProxy>();
		private ITypeFilter _typeFilter;
		private DocumentationCache _documentation;
		private SwiftTranslator _translator;
		private AppDomainSetup _targetDomainSetup;
		private AppDomain _targetDomain;
		private string _selfLocation;
		private AppDomainProxy _targetDomainProxy;
		private bool _swift12;
	}
}

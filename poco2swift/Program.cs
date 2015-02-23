using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

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
			LoadAssemblies();

			_typeFilter = new DataContractFilter(_configuration);
			_documentation = new DocumentationCache();
			_translator = new SwiftTranslator(_configuration, _typeFilter, _documentation);

			ReadTypes();
			TranslateTypes();
			WriteSwiftSource();
		}

		private void LoadAssemblies()
		{
			foreach (var path in _dllPaths)
			{
				var assembly = Assembly.LoadFile(path);

				assembly.ModuleResolve += HandleModuleResolve;

				_assemblies.Add(assembly);
			}
		}

		private static Module HandleModuleResolve(object sender, ResolveEventArgs e)
		{
			ErrorHandler.Error("Cannot resolve name '{0}' from assembly '{1}'", e.Name, e.RequestingAssembly.FullName);

			return null;
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
					}
				}
			}

			if (configuration.imports == null)
				configuration.imports = new ModuleType[0];

			if (configuration.skiptypes == null)
				configuration.skiptypes = new SkipType[0];

			if (configuration.externaltypes == null)
				configuration.externaltypes = new ExternalType[0];

			if (configuration.enumerations == null)
				configuration.enumerations = new EnumType[0];

			if (configuration.classes == null)
				configuration.classes = new ClassType[0];

			_configuration = configuration;

			foreach (var @enum in configuration.enumerations)
			{
				_configuredEnums.Add(@enum.name, @enum);
			}

			foreach (var @class in configuration.classes)
			{
				_configuredClasses.Add(@class.name, @class);
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
			if (configType.includeSpecified && configType.include)
			{
				if (String.IsNullOrEmpty(configType.fullname))
				{
					ErrorHandler.Fatal(ErrorHandler.MISSING_FULL_NAME, "Included {0} '{1}' is missing the full-name attribute.", typeName, configType.name);
				}

				var type = Type.GetType(configType.fullname);

				ReadType(type, forceGoodType: true);
			}
		}

		private void ReadType(Type type, bool forceGoodType = false)
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
				_translator.TranslateType(type, forDefinition: true);
			}
		}

		private void WriteSwiftSource()
		{
			using (var writer = new SwiftWriter(_outputDir, _configuration))
			{
				foreach (var tuple in _translator.GetCachedSwiftTypes())
				{
					var swiftType = tuple.Item1;
					var type = tuple.Item2;

					writer.Write(swiftType, type);
				}
			}
		}

		private Program(string[] args)
		{
			ParseArgs(args);
		}

		private void ParseArgs(string[] args)
		{
			for (int argIndex = 0; argIndex < args.Length; ++argIndex)
			{
				string arg = args[argIndex];

				if (arg.Substring(0, 1).IndexOfAny(new char[] { '-', '/' }) == 0)
				{
					switch (arg.Substring(1).ToLowerInvariant())
					{
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

						default:
							Usage();
							break;
					}
				}
			}

			if (!_dllPaths.Any())
				Usage();

			if (String.IsNullOrEmpty(_outputDir))
				Usage();
		}

		private void Usage()
		{
			Console.Out.WriteLine("mmwfup [-d] [-o <path>] [-hack_60_70] [-update_v1]");
			Console.Out.WriteLine("   -d          Dump workflow instances.");
			Console.Out.WriteLine("   -l <path>   Path to the log file.");
			Console.Out.WriteLine("   -o <path>   Directory to receive the dumped workflow instances.");
			Console.Out.WriteLine("   -hack_60_70 Hack the serialized workflow instances to work around compatibility bugs in Dme classes between 4.0.6x and 4.0.7x.");
			Console.Out.WriteLine("   -update_v1  Update all workflow instances to version 1.");

			Environment.Exit(20);
		}

		private IList<string> _dllPaths = new List<string>();
		private string _outputDir = ".";
		private string _configPath;
		private IList<Assembly> _assemblies = new List<Assembly>();
		private Poco2SwiftType _configuration;
		private IDictionary<string, EnumType> _configuredEnums = new Dictionary<string, EnumType>();
		private IDictionary<string, ClassType> _configuredClasses = new Dictionary<string, ClassType>();
		private ISet<Type> _types = new HashSet<Type>();
		private ITypeFilter _typeFilter;
		private DocumentationCache _documentation;
		private SwiftTranslator _translator;
	}
}

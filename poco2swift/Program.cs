using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using poco2swift.SwiftTypes;

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

		//private void EmitEnumerations()
		//{
		//	foreach (var enumType in _enumerations)
		//	{
		//		EmitEnumeration(enumType);
		//	}
		//}

		//private void EmitClasses()
		//{
		//	foreach (var classType in _classes)
		//	{
		//		EmitClass(classType);
		//	}
		//}

		//private void EmitEnumeration(Type enumType)
		//{
		//	var contract = enumType.GetCustomAttribute(typeof(DataContractAttribute)) as DataContractAttribute;

		//	_output.WriteLine();

		//	// Enum documentation

		//	EmitTypeSummaryComment(enumType);

		//	// Enum declaration

		//	var outputName = ((contract != null) && !String.IsNullOrEmpty(contract.Name)) ? contract.Name : TranslateType(enumType);

		//	var rawType = Enum.GetUnderlyingType(enumType);

		//	_output.WriteLine("enum {0}: {1} {{", outputName, TranslateType(rawType));

		//	bool firstValue = true;

		//	foreach (var value in Enum.GetValues(enumType))
		//	{
		//		var valueName = Enum.GetName(enumType, value);

		//		if (!firstValue)
		//			_output.WriteLine();

		//		EmitFieldSummaryComment(enumType, valueName);

		//		var rawValue = Convert.ChangeType(value, rawType);

		//		_output.WriteLine("\tcase {0} = {1}", valueName, TranslateValue(rawValue));

		//		firstValue = false;
		//	}

		//	_output.WriteLine("}");
		//}

		//private void EmitClass(Type classType)
		//{
		//	var contract = classType.GetCustomAttribute(typeof(DataContractAttribute)) as DataContractAttribute;

		//	if (contract == null)
		//		return;

		//	_output.WriteLine();

		//	// Class documentation

		//	EmitTypeSummaryComment(classType);

		//	// Class declaration

		//	var outputName = String.IsNullOrEmpty(contract.Name) ? TranslateType(classType) : contract.Name;

		//	if (classType.BaseType != typeof(object))
		//	{
		//		var baseName = TranslateType(classType.BaseType);

		//		_output.WriteLine("class {0}: {1} {{", outputName, baseName);
		//	}
		//	else
		//	{
		//		_output.WriteLine("class {0} {{", outputName);
		//	}

		//	// Members

		//	bool firstProperty = true;

		//	foreach (var prop in ReadProperties(classType))
		//	{
		//		EmitClassProperty(prop.Item1, prop.Item2, firstProperty);

		//		firstProperty = false;
		//	}

		//	_output.WriteLine("}");
		//}

		//private string PrintTypeName(Type type)
		//{
		//	if (type.IsGenericType)
		//	{
		//		//var genargs = type.GetGenericArguments();
		//		////var genpc = type.GetGenericParameterConstraints();
		//		//var gentd = type.GetGenericTypeDefinition();

		//		//Console.Out.WriteLine(gentd.Name);

		//		var name = type.Name;
		//		int backquote = name.IndexOf('`');

		//		if (backquote >= 0)
		//			name = name.Substring(0, backquote);

		//		var genericArgs = String.Join(",", from arg in type.GetGenericArguments() select arg.Name);

		//		return String.Format("{0}<{1}>", name, genericArgs);
		//	}

		//	return type.FullName;
		//}

		//private string PrintTypeFullName(Type type)
		//{
		//	return String.Format("{0}.{1}", type.Namespace, PrintTypeName(type));
		//}

		//private IEnumerable<Tuple<PropertyInfo,DataMemberAttribute>> ReadProperties(Type classType)
		//{
		//	var properties = new List<Tuple<PropertyInfo, DataMemberAttribute>>();

		//	foreach (var property in classType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
		//	{
		//		if (property.DeclaringType != classType)
		//			continue;

		//		var contract = property.GetCustomAttribute(typeof(DataMemberAttribute)) as DataMemberAttribute;

		//		if (contract == null)
		//			continue;

		//		properties.Add(new Tuple<PropertyInfo, DataMemberAttribute>(property, contract));
		//	}

		//	properties.Sort((Tuple<PropertyInfo, DataMemberAttribute> p1, Tuple<PropertyInfo, DataMemberAttribute> p2) =>
		//	{
		//		var order1 = p1.Item2.Order;
		//		var order2 = p2.Item2.Order;

		//		if ((order1 >= 0) && (order2 >= 0))
		//			return order1.CompareTo(order2);
		//		else if (order1 >= 0)
		//			return 1;
		//		else if (order2 >= 0)
		//			return -1;
		//		else
		//			return p1.Item1.Name.CompareTo(p2.Item1.Name);
		//	});

		//	return properties;
		//}

		//private void EmitClassProperty(PropertyInfo property, DataMemberAttribute contract, bool firstProperty)
		//{
		//	var name = String.IsNullOrEmpty(contract.Name) ? property.Name : contract.Name;
		//	var propertyType = property.PropertyType;

		//	if (!firstProperty)
		//		_output.WriteLine();

		//	// Property documentation

		//	EmitPropertySummaryComment(property);

		//	var typeName = TranslateType(propertyType);

		//	if (!contract.IsRequired)
		//	{
		//		if (typeName.Substring(typeName.Length-1) != "?")
		//		{
		//			typeName += "?";
		//		}
		//	}
			
		//	if (typeName.Substring(typeName.Length - 1) != "?")
		//	{
		//		var defaultValue = GetDefaultValue(propertyType);

		//		_output.WriteLine("\tvar {0}: {1} = {2}", name, typeName, TranslateValue(defaultValue));
		//	}
		//	else
		//	{
		//		_output.WriteLine("\tvar {0}: {1}", name, typeName);
		//	}
		//}

		//private void EmitTypeSummaryComment(Type type)
		//{
		//	if (_documentation != null)
		//	{
		//		var xpath = String.Format("/doc/members/member[@name = 'T:{0}']/summary", type.FullName);

		//		EmitSummaryComment(0, _documentation.SelectSingleNode(xpath));
		//	}
		//}

		//private void EmitPropertySummaryComment(PropertyInfo property)
		//{
		//	if (_documentation != null)
		//	{
		//		var xpath = String.Format("/doc/members/member[@name = 'P:{0}.{1}']/summary", property.DeclaringType.FullName, property.Name);

		//		EmitSummaryComment(1, _documentation.SelectSingleNode(xpath));
		//	}
		//}

		//private void EmitFieldSummaryComment(Type type, string memberName)
		//{
		//	if (_documentation != null)
		//	{
		//		var xpath = String.Format("/doc/members/member[@name = 'F:{0}.{1}']/summary", type, memberName);

		//		EmitSummaryComment(1, _documentation.SelectSingleNode(xpath));
		//	}
		//}

		//private void EmitSummaryComment(int indent, XmlNode node)
		//{
		//	if (node == null)
		//		return;

		//	var summary = node.InnerText.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

		//	foreach (var line in summary)
		//	{
		//		_output.WriteLine("{0}/// {1}", new string('\t', indent), line.Trim());
		//	}
		//}

		private string TranslateType(Type type)
		{
			string primitiveType;

			if (type.IsArray)
			{
				var innerType = type.GetElementType();
				var innerSwiftType = TranslateType(innerType);

				return String.Format(CultureInfo.InvariantCulture, "[{0}]", innerSwiftType);
			}
			else if (type.IsGenericType)
			{
				var typeArgs = type.GetGenericArguments();

				if (typeArgs.Length == 1)
				{
					var innerType = typeArgs[0];
					var innerSwiftType = TranslateType(innerType);

					if (innerType.IsValueType && (type == typeof(Nullable<>).MakeGenericType(typeArgs)))
					{
						return String.Format(CultureInfo.InvariantCulture, "{0}?", innerSwiftType);
					}
					else if (type == typeof(IEnumerable<>).MakeGenericType(typeArgs))
					{
						return String.Format(CultureInfo.InvariantCulture, "[{0}]", innerSwiftType);
					}
				}
				else if (typeArgs.Length == 2)
				{
					var innerSwiftType0 = TranslateType(typeArgs[0]);
					var innerSwiftType1 = TranslateType(typeArgs[1]);

					if (type == typeof(IDictionary<,>).MakeGenericType(typeArgs))
					{
						return String.Format(CultureInfo.InvariantCulture, "[{0}: {1}]", innerSwiftType0, innerSwiftType1);
					}
				}

				var name = type.Name;
				int backquote = name.IndexOf('`');

				if (backquote >= 0)
					name = name.Substring(0, backquote);

				var genericArgs = String.Join(",", from arg in typeArgs select arg.Name);

				return String.Format(CultureInfo.InvariantCulture, "{0}<{1}>", name, genericArgs);
			}
			//else if (_PrimitiveTypes.TryGetValue(type, out primitiveType))
			//{
			//	return primitiveType;
			//}

			return type.Name;
		}

		private object GetDefaultValue(Type type)
		{
			if (type.IsValueType)
			{
				if (type.IsEnum)
				{
					return Enum.GetValues(type).Cast<object>().First();
				}
				else if (type == typeof(bool))
				{
					return false;
				}
				else if (
					(type == typeof(sbyte))  || 
					(type == typeof(byte))   || 
					(type == typeof(Int16))  || 
					(type == typeof(UInt16)) || 
					(type == typeof(Int32))  || 
					(type == typeof(UInt32)) || 
					(type == typeof(Int64))  || 
					(type == typeof(UInt64)))
				{
					return 0;
				}
				else if (
					(type == typeof(float)) || 
					(type == typeof(double)))
				{
					return 0.0;
				}
				else if (type == typeof(char))
				{
					return '\0';
				}
			}
			else if (type.IsClass)
			{
				if (type == typeof(string))
				{
					return "";
				}
			}

			return null;
		}

		private string TranslateValue(object value)
		{
			if (value == null)
				return "nil";
			else if (value is bool)
				return ((bool)value) ? "true" : "false";
			else if (value is string)
				return String.Format(CultureInfo.InvariantCulture, "\"{0}\"", value);
			else if (value is char)
				return String.Format(CultureInfo.InvariantCulture, "'{0}'", value);
			else if (value is Enum)
				return String.Format(CultureInfo.InvariantCulture, ".{0}", Enum.GetName(value.GetType(), value));
			else
				return String.Format("{0}", value);
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

						//case "g":
						//	if (argIndex < args.Length - 1)
						//	{
						//		arg2 = args[++argIndex];

						//		if ((arg2.Length > 0) && (arg2[0] != '^'))
						//			arg2 = "^" + arg2;

						//		if ((arg2.Length > 0) && (arg2[arg2.Length - 1] != '$'))
						//			arg2 += "$";

						//		_ignoredTypes.Add(new Regex(arg2, RegexOptions.ECMAScript));
						//	}
						//	else
						//		Usage();
						//	break;

						case "o":
							if (argIndex < args.Length - 1)
								_outputDir = args[++argIndex];
							else
								Usage();
							break;

						//case "x":
						//	if (argIndex < args.Length - 1)
						//		_xmlPath = args[++argIndex];
						//	else
						//		Usage();
						//	break;

						default:
							Usage();
							break;
					}
				}
			}

			if (_dllPaths.Count < 1)
				Usage();

			if (String.IsNullOrEmpty(_outputDir))
				Usage();

			//if (String.IsNullOrEmpty(_xmlPath))
			//	_xmlPath = Path.ChangeExtension(_dllPath, ".xml");

			//if (String.IsNullOrEmpty(_swiftPath))
			//	_swiftPath = Path.ChangeExtension(_dllPath, ".swift");
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
		//private string _xmlPath;
		private string _outputDir = ".";
		private string _configPath;
		private IList<Assembly> _assemblies = new List<Assembly>();
		//private Assembly _assembly;
		//private XmlDocument _documentation;
		//private TextWriter _output;
		private Poco2SwiftType _configuration;
		private IDictionary<string, EnumType> _configuredEnums = new Dictionary<string, EnumType>();
		private IDictionary<string, ClassType> _configuredClasses = new Dictionary<string, ClassType>();
		private ISet<Type> _types = new HashSet<Type>();
		//private IDictionary<string, SwiftType> _swiftTypes = new Dictionary<string, SwiftType>();
		//private IList<Type> _enumerations;
		//private IList<Type> _classes;
		private ITypeFilter _typeFilter;
		private DocumentationCache _documentation;
		private SwiftTranslator _translator;

		//private static readonly IDictionary<Type, string> _PrimitiveTypes = new Dictionary<Type, string>
		//{
		//	{ typeof(SByte), "Int8" },
		//	{ typeof(Int16), "Int16" },
		//	{ typeof(Int32), "Int32" },
		//	{ typeof(Int64), "Int64" },
		//	{ typeof(Byte), "UInt8" },
		//	{ typeof(UInt16), "UInt16" },
		//	{ typeof(UInt32), "UInt32" },
		//	{ typeof(UInt64), "UInt64" },
		//	{ typeof(string), "String" },
		//	{ typeof(bool), "Bool" },
		//	{ typeof(float), "Float" },
		//	{ typeof(double), "Double" },
		//};
	}
}

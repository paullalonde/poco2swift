using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using poco2swift.SwiftTypes;

namespace poco2swift
{
	public class SwiftTranslator
	{
		public SwiftTranslator(Poco2SwiftType configuration, ITypeFilter filter, DocumentationCache documentation)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");

			if (filter == null)
				throw new ArgumentNullException("filter");

			if (documentation == null)
				throw new ArgumentNullException("documentation");

			_configuration = configuration;
			_filter = filter;
			_documentation = documentation;

			CacheMappedTypes();
		}

		public void CacheSwiftName(Type type, string swiftName)
		{
			_swiftNamesToTypes.Add(swiftName, type);
			_swiftTypesToNames.Add(type, swiftName);
		}

		public IEnumerable<Tuple<SwiftType, Type>> GetCachedSwiftTypes()
		{
			var swiftTypes = new List<Tuple<SwiftType, Type>>();

			foreach (var kvp in from x in _swiftNamesToTypes orderby x.Key select x)
			{
				var type = kvp.Value;
				SwiftType swiftType;

				if (_swiftTypes.TryGetValue(type, out swiftType))
					swiftTypes.Add(new Tuple<SwiftType,Type>(swiftType, type));
				else
					ErrorHandler.Error("Missing Swift type for type '{0}'.", type.FullName);
			}

			return swiftTypes;
		}

		public SwiftType TranslateType(Type type, bool forDefinition = false)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			//Console.Out.WriteLine("Translating type {0}", type.FullName);

			SwiftType swiftType;

			if (_swiftTypes.TryGetValue(type, out swiftType))
				return swiftType;

			if (type.IsGenericParameter)
			{
				swiftType = TranslateGenericParameter(type);
			}
			else if (type.IsArray)
			{
				swiftType = TranslateArray(type);
			}
			else if (type.IsEnum)
			{
				swiftType = TranslateEnum(type);
			}
			else if (type.IsInterface)
			{
				swiftType = TranslateInterface(type);
			}
			else if (type.IsClass || type.IsValueType)
			{
				swiftType = TranslateClass(type, forDefinition);
			}
			else
			{
				ErrorHandler.Error("Skipping undefined type '{0}'.", type.FullName);
			}

			return swiftType;
		}

		private SwiftType TranslateGenericParameter(Type parameterType)
		{
			var swiftType = new SwiftPlaceholder(parameterType.Name);

			_swiftTypes.Add(parameterType, swiftType);

			return swiftType;
		}

		private SwiftType TranslateArray(Type arrayType)
		{
			//Console.Out.WriteLine("Translating array {0}", arrayType.FullName);

			var elementType = TranslateType(arrayType.GetElementType());

			if (elementType == null)
				return null;

			var swiftType = new SwiftArray(elementType);

			_swiftTypes.Add(arrayType, swiftType);

			return swiftType;
		}

		private SwiftType TranslateInterface(Type classType)
		{
			return TranslateWellKnownType(classType);
		}

		//private SwiftType TranslateClassDefinition(Type classType)
		//{

		//}

		private SwiftType TranslateClass(Type classType, bool forDefinition)
		{
			//Console.Out.WriteLine("Translating class {0}", classType.FullName);

			var swiftType = TranslateWellKnownType(classType);

			if (swiftType != null)
				return swiftType;

			string name;

			if (classType.IsGenericType && !classType.IsGenericTypeDefinition)
			{
				var definitionType = classType.GetGenericTypeDefinition();

				if (!_swiftTypesToNames.TryGetValue(definitionType, out name))
				{
					ErrorHandler.Error("Skipping undefined generic type definition '{0}'.", definitionType.FullName);

					return null;
				}
			}
			else
			{
				if (!_swiftTypesToNames.TryGetValue(classType, out name))
				{
					ErrorHandler.Error("Skipping undefined class '{0}'.", classType.FullName);

					return null;
				}
			}

			var swiftClass = new SwiftClass(name)
			{
				BriefComment = _documentation.GetTypeSummary(classType),
			};

			_swiftTypes.Add(classType, swiftClass);

			var baseType = classType.BaseType;

			if (baseType != typeof(object))
			{
				var baseSwiftType = TranslateType(baseType);

				if (baseSwiftType == null)
				{
					ErrorHandler.Error("Skipping class '{0}' because of undefined base class '{1}'.", classType.FullName, baseType.FullName);

					return null;
				}

				swiftClass.BaseType = baseSwiftType;
			}

			if (classType.IsGenericType)
			{
				if (classType.IsGenericTypeDefinition)
				{
					foreach (var typeArg in classType.GetGenericArguments())
					{
						if (typeArg.IsGenericParameter)
						{
							swiftClass.AddTypeParameter(new SwiftPlaceholder(typeArg.Name));
						}
						else
						{
							throw new InvalidOperationException();
						}

						//var swiftTypeArg = TranslateType(typeArg);

						//if (swiftTypeArg == null)
						//{
						//	ErrorHandler.Error("Skipping generic parameter of class '{0}' because of undefined type '{1}'.", classType.Name, typeArg.FullName);

						//	continue;
						//}

						//swiftClass.AddTypeParameter(typeArg.Name, swiftTypeArg);
					}
				}

				//foreach (var typeArg in classType.GetGenericArguments())
				//{
				//	var swiftTypeArg = TranslateType(typeArg);

				//	if (swiftTypeArg == null)
				//	{
				//		ErrorHandler.Error("Skipping generic parameter of class '{0}' because of undefined class '{1}'.", classType.Name, typeArg.FullName);

				//		continue;
				//	}

				//	//swiftClass.AddTypeParameter(typeArg.Name, swiftTypeArg);
				//}
			}

			foreach (var property in ReadProperties(classType))
			{
				//Console.Out.WriteLine("Translating property {0} of class {1}", property.Name, classType.FullName);

				var swiftPropertyType = TranslateType(property.PropertyType);

				if (swiftPropertyType == null)
				{
					ErrorHandler.Error("Skipping property of undefined type '{0}.{1}'.", property.DeclaringType.FullName, property.Name);

					continue;
				}

				if (!swiftPropertyType.IsOptional)
					swiftPropertyType = new SwiftOptional(swiftPropertyType, true);

				var propertyName = _filter.GetPropertyName(property);

				var swiftProperty = new SwiftProperty(propertyName, swiftPropertyType)
				{
					BriefComment = _documentation.GetPropertySummary(property),
				};

				swiftClass.AddProperty(swiftProperty);
			}

			return swiftClass;
		}

		private SwiftType TranslateWellKnownType(Type classType)
		{
			//Console.Out.WriteLine("Translating class {0}", classType.FullName);

			if (classType.IsGenericType)
			{
				var typeArgs = classType.GetGenericArguments();

				if (typeArgs.Length == 1)
				{
					var innerType = typeArgs[0];

					if (innerType.IsValueType && (classType == typeof(Nullable<>).MakeGenericType(typeArgs)))
					{
						return TranslateNullable(classType, innerType);
					}
					else if (typeof(IEnumerable<>).MakeGenericType(typeArgs).IsAssignableFrom(classType))
					{
						return TranslateList(classType, innerType);
					}
				}
				else if (typeArgs.Length == 2)
				{
					if (typeof(IDictionary<,>).MakeGenericType(typeArgs).IsAssignableFrom(classType))
					{
						return TranslateDictionary(classType, typeArgs[0], typeArgs[1]);
					}
				}
			}

			return null;
		}


		private SwiftType TranslateNullable(Type nullableType, Type innerType)
		{
			// Nullable<T> gets translated into Optional<T>

			var innerSwiftType = TranslateType(innerType);

			if (innerSwiftType == null)
				return null;

			var swiftType = new SwiftOptional(innerSwiftType);

			_swiftTypes.Add(nullableType, swiftType);

			return swiftType;
		}

		private SwiftType TranslateList(Type collectionType, Type elementType)
		{
			// IEnumerable<T> gets translated into [T]

			var elementSwiftType = TranslateType(elementType);

			if (elementSwiftType == null)
				return null;

			var swiftType = new SwiftArray(elementSwiftType);

			_swiftTypes.Add(collectionType, swiftType);

			return swiftType;
		}

		private SwiftType TranslateDictionary(Type collectionType, Type keyType, Type valueType)
		{
			// IDictionary<K,V> gets translated into [K: V]

			var keySwiftType = TranslateType(keyType);

			if (keySwiftType == null)
				return null;

			var valueSwiftType = TranslateType(valueType);

			if (valueSwiftType == null)
				return null;

			var swiftType = new SwiftDictionary(keySwiftType, valueSwiftType);

			_swiftTypes.Add(collectionType, swiftType);

			return swiftType;
		}

		private IEnumerable<PropertyInfo> ReadProperties(Type classType)
		{
			var properties = new List<PropertyInfo>();

			foreach (var property in classType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				if (property.DeclaringType != classType)
					continue;

				if (!_filter.IsGoodProperty(property))
					continue;

				properties.Add(property);
			}

			return _filter.SortProperties(properties);
		}

		private SwiftType TranslateEnum(Type enumType)
		{
			string name;

			if (!_swiftTypesToNames.TryGetValue(enumType, out name))
			{
				ErrorHandler.Error("Skipping undefined enum '{0}'.", enumType.FullName);

				return null;
			}

			var swiftEnum = new SwiftEnum(name)
			{
				BriefComment = _documentation.GetTypeSummary(enumType),
			};

			var baseType = Enum.GetUnderlyingType(enumType);

			if (baseType != typeof(object))
			{
				var baseSwiftType = TranslateType(baseType);

				if (baseType == null)
				{
					ErrorHandler.Error("Skipping enum '{0}' because of undefined base class '{1}'.", enumType.FullName, baseType.FullName);

					return null;
				}

				swiftEnum.BaseType = baseSwiftType;
			}

			_swiftTypes.Add(enumType, swiftEnum);

			var rawType = Enum.GetUnderlyingType(enumType);

			foreach (var value in Enum.GetValues(enumType).Cast<Enum>())
			{
				var valueName = Enum.GetName(enumType, value);
				var member = enumType.GetMember(valueName)[0];
				var swiftValueName = _filter.GetEnumName(value);
				var underlyingValue = _filter.GetUnderlyingEnumValue(value);

				var swiftValue = new SwiftEnumValue(swiftValueName, underlyingValue)
				{
					BriefComment = _documentation.GetFieldSummary(member),
				};

				swiftEnum.AddValue(swiftValue);
			}

			return swiftEnum;
		}

		private void CacheMappedTypes()
		{
			if (_configuration.externaltypes != null)
			{
				foreach (var externalType in _configuration.externaltypes)
				{
					if (String.IsNullOrEmpty(externalType.fullname))
					{
						ErrorHandler.Error("Empty 'fullname' in map-type");
						continue;
					}

					if (String.IsNullOrEmpty(externalType.swiftname))
					{
						ErrorHandler.Error("Empty 'swift-name' in map-type");
						continue;
					}

					var type = Type.GetType(externalType.fullname);
					var swiftType = new SwiftClass(externalType.swiftname);

					_swiftTypes.Add(type, swiftType);
				}
			}

			// Add predefined map types.

			foreach (var kvp in _PredefinedMapTypes)
			{
				if (!_swiftTypes.ContainsKey(kvp.Key))
					_swiftTypes.Add(kvp.Key, kvp.Value);
			}
		}

		private readonly Poco2SwiftType _configuration;
		private readonly ITypeFilter _filter;
		private readonly DocumentationCache _documentation;
		private IDictionary<Type, SwiftType> _swiftTypes = new Dictionary<Type, SwiftType>();
		private IDictionary<string, Type> _swiftNamesToTypes = new Dictionary<string, Type>();
		private IDictionary<Type, string> _swiftTypesToNames = new Dictionary<Type, string>();

		private static readonly IDictionary<Type, SwiftClass> _PredefinedMapTypes = new Dictionary<Type, SwiftClass>
		{
			{ typeof(object), new SwiftClass("Any")    },
			{ typeof(SByte),  new SwiftClass("Int8")   },
			{ typeof(Int16),  new SwiftClass("Int16")  },
			{ typeof(Int32),  new SwiftClass("Int32")  },
			{ typeof(Int64),  new SwiftClass("Int64")  },
			{ typeof(Byte),   new SwiftClass("UInt8")  },
			{ typeof(UInt16), new SwiftClass("UInt16") },
			{ typeof(UInt32), new SwiftClass("UInt32") },
			{ typeof(UInt64), new SwiftClass("UInt64") },
			{ typeof(string), new SwiftClass("String") },
			{ typeof(bool),   new SwiftClass("Bool")   },
			{ typeof(float),  new SwiftClass("Float")  },
			{ typeof(double), new SwiftClass("Double") },
			//{ typeof(Guid),   new SwiftPrimitive(TypeCode.NSUUID) },
			//{ typeof(Uri),    new SwiftPrimitive(TypeCode.NSURL)  },
		};
	}
}

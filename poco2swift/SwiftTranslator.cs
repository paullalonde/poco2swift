using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using poco2swift.probe;
using poco2swift.SwiftTypes;

namespace poco2swift
{
	public class SwiftTranslator
	{
		public SwiftTranslator(Poco2SwiftType configuration, ITypeFilter filter, DocumentationCache documentation, IAppDomainProxy appDomain)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");

			if (filter == null)
				throw new ArgumentNullException("filter");

			if (documentation == null)
				throw new ArgumentNullException("documentation");

			if (appDomain == null)
				throw new ArgumentNullException("appDomain");

			_configuration = configuration;
			_filter = filter;
			_documentation = documentation;
			_appDomain = appDomain;

			InitPredefinedMapTypes();
			CacheMappedTypes();
		}

		private void InitPredefinedMapTypes()
		{
			AddPredefinedMapType(typeof(object), new SwiftClass("Any"));
			AddPredefinedMapType(typeof(SByte), new SwiftClass("Int8"));
			AddPredefinedMapType(typeof(Int16), new SwiftClass("Int16"));
			AddPredefinedMapType(typeof(Int32), new SwiftClass("Int32"));
			AddPredefinedMapType(typeof(Int64), new SwiftClass("Int64"));
			AddPredefinedMapType(typeof(Byte), new SwiftClass("UInt8"));
			AddPredefinedMapType(typeof(UInt16), new SwiftClass("UInt16"));
			AddPredefinedMapType(typeof(UInt32), new SwiftClass("UInt32"));
			AddPredefinedMapType(typeof(UInt64), new SwiftClass("UInt64"));
			AddPredefinedMapType(typeof(string), new SwiftClass("String"));
			AddPredefinedMapType(typeof(bool), new SwiftClass("Bool"));
			AddPredefinedMapType(typeof(float), new SwiftClass("Float"));
			AddPredefinedMapType(typeof(double), new SwiftClass("Double"));
			//AddPredefinedMapType(typeof(Guid),   new SwiftPrimitive(TypeCode.NSUUID));
			//AddPredefinedMapType(typeof(Uri),    new SwiftPrimitive(TypeCode.NSURL));
		}

		public void CacheSwiftName(TypeProxy type, string swiftName)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			if (String.IsNullOrEmpty(swiftName))
				throw new ArgumentNullException("swiftName");

			TypeProxy foundType;

			if (!_swiftNamesToTypes.TryGetValue(swiftName, out foundType))
			{
				_swiftNamesToTypes.Add(swiftName, type);
				_swiftTypesToNames.Add(type, swiftName);
			}
			else if (foundType != type)
			{
				ErrorHandler.Error("Swift type '{0}' has multiple source types.", swiftName);
			}
		}

		public IEnumerable<Tuple<SwiftType, TypeProxy>> GetCachedSwiftTypes()
		{
			var swiftTypes = new List<Tuple<SwiftType, TypeProxy>>();

			foreach (var kvp in from x in _swiftNamesToTypes orderby x.Key select x)
			{
				var type = kvp.Value;
				SwiftType swiftType;

				if (_swiftTypes.TryGetValue(type, out swiftType))
					swiftTypes.Add(new Tuple<SwiftType, TypeProxy>(swiftType, type));
				else
					ErrorHandler.Error("Missing Swift type for type '{0}'.", type.FullName);
			}

			return swiftTypes;
		}

		public SwiftType TranslateType(TypeProxy type)
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
				swiftType = TranslateClass(type);
			}
			else
			{
				ErrorHandler.Error("Skipping undefined type '{0}'.", type.FullName);
			}

			return swiftType;
		}

		private SwiftType TranslateGenericParameter(TypeProxy parameterType)
		{
			var swiftType = new SwiftPlaceholder(parameterType.Name);

			_swiftTypes.Add(parameterType, swiftType);

			return swiftType;
		}

		private SwiftType TranslateArray(TypeProxy arrayType)
		{
			//Console.Out.WriteLine("Translating array {0}", arrayType.FullName);

			var elementType = TranslateType(arrayType.GetElementType());

			if (elementType == null)
				return null;

			var swiftType = new SwiftArray(elementType);

			_swiftTypes.Add(arrayType, swiftType);

			return swiftType;
		}

		private SwiftType TranslateInterface(TypeProxy classType)
		{
			return TranslateWellKnownType(classType);
		}

		//private SwiftType TranslateClassDefinition(Type classType)
		//{

		//}

		private SwiftType TranslateClass(TypeProxy classType)
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

			var baseType = (classType.BaseType != _appDomain.ObjectType) ? classType.BaseType : null;

			if (baseType != null)
			{
				if (!classType.IsGenericType && baseType.IsGenericType)
				{
					// The class is not generic, but its base class is.
					// This isn't a valid Swift construct, so we need to convert it.
					// 
					// Given these C# classes :
					//		class Base<T> {}
					//		class Derived : Base<int> {}
					//
					// We need to generate these Swift classes :
					//		class Base<T> {}

					var baseArgs = baseType.GetGenericArguments();
					var templateType = baseType.GetGenericTypeDefinition();
					var templateArgs = templateType.GetGenericArguments();

					for (int i = 0; i < baseArgs.Count; ++i)
					{
						var baseArg = baseArgs[i];
						var templateArg = templateArgs[i];
						string parameterName;

						if (baseArg.IsGenericParameter)
							parameterName = baseArg.Name;
						else
							parameterName = templateArg.Name;

						var swiftBaseArg = TranslateType(baseArg);

						if (swiftBaseArg == null)
						{
							ErrorHandler.Error("Skipping generic parameter of class '{0}' because of undefined type '{1}'.", baseType.Name, baseArg.FullName);

							return null;
						}

						var swiftTemplateArg = TranslateType(templateArg);

						if (swiftTemplateArg == null)
						{
							ErrorHandler.Error("Skipping generic parameter of class '{0}' because of undefined type '{1}'.", templateType.Name, templateArg.FullName);

							return null;
						}

						swiftClass.AddTypeParameter(parameterName, swiftTemplateArg);
						swiftClass.AddTypeParameterContraint(parameterName, swiftBaseArg);
					}

					baseType = templateType;
				}

				//var generic = classType.IsGenericType;
				//var genericTypeDef = classType.IsGenericTypeDefinition;
				//var base_eneric = baseType.IsGenericType;
				//var base_genericTypeDef = baseType.IsGenericTypeDefinition;

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

							foreach (var constraintType in typeArg.GetGenericParameterConstraints())
							{
								if (!constraintType.IsClass)
									continue;

								// A constraint on 'struct' is expressed as a constraint on System.ValueType.

								if (constraintType == _appDomain.ValueType)
									continue;

								var constraintSwiftClass = TranslateType(constraintType);

								swiftClass.AddTypeParameterContraint(typeArg.Name, constraintSwiftClass);

								break;
							}
						}
						else
						{
							throw new InvalidOperationException();
						}
					}
				}
				else
				{
					var templateType = classType.GetGenericTypeDefinition();
					var templateArgs = templateType.GetGenericArguments();
					var classArgs = classType.GetGenericArguments();

					for (int i = 0; i < classArgs.Count; ++i)
					{
						var classArg = classArgs[i];
						var templateArg = templateArgs[i];
						string parameterName;

						if (classArg.IsGenericParameter)
							parameterName = classArg.Name;
						else
							parameterName = templateArg.Name;

						var swiftClassArg = TranslateType(classArg);

						if (swiftClassArg == null)
						{
							ErrorHandler.Error("Skipping generic parameter of class '{0}' because of undefined type '{1}'.", classType.Name, classArg.FullName);

							continue;
						}

						swiftClass.AddTypeParameter(parameterName, swiftClassArg);
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

		private SwiftType TranslateWellKnownType(TypeProxy classType)
		{
			//Console.Out.WriteLine("Translating class {0}", classType.FullName);

			if (classType.IsGenericType)
			{
				var typeArgs = classType.GetGenericArguments();

				if (typeArgs.Count == 1)
				{
					var innerType = typeArgs[0];

					if (innerType.IsValueType && (classType == _appDomain.MakeGenericNullableType(innerType)))
					{
						return TranslateNullable(classType, innerType);
					}
					else if (_appDomain.MakeGenericSetType(innerType).IsAssignableFrom(classType))
					{
						return TranslateSet(classType, innerType);
					}
					else if (_appDomain.MakeGenericEnumerableType(innerType).IsAssignableFrom(classType))
					{
						return TranslateList(classType, innerType);
					}
				}
				else if (typeArgs.Count == 2)
				{
					var keyType = typeArgs[0];
					var valueType = typeArgs[1];

					if (_appDomain.MakeGenericDictionaryType(keyType, valueType).IsAssignableFrom(classType))
					{
						return TranslateDictionary(classType, keyType, valueType);
					}
				}
			}

			return null;
		}


		private SwiftType TranslateNullable(TypeProxy nullableType, TypeProxy innerType)
		{
			// Nullable<T> gets translated into Optional<T>

			var innerSwiftType = TranslateType(innerType);

			if (innerSwiftType == null)
				return null;

			var swiftType = new SwiftOptional(innerSwiftType);

			_swiftTypes.Add(nullableType, swiftType);

			return swiftType;
		}

		private SwiftType TranslateList(TypeProxy collectionType, TypeProxy elementType)
		{
			// IEnumerable<T> gets translated into [T]

			var elementSwiftType = TranslateType(elementType);

			if (elementSwiftType == null)
				return null;

			var swiftType = new SwiftArray(elementSwiftType);

			_swiftTypes.Add(collectionType, swiftType);

			return swiftType;
		}

		private SwiftType TranslateSet(TypeProxy collectionType, TypeProxy elementType)
		{
			// ISet<T> gets translated into Set<T>

			var elementSwiftType = TranslateType(elementType);

			if (elementSwiftType == null)
				return null;

			var swiftType = new SwiftSet(elementSwiftType);

			_swiftTypes.Add(collectionType, swiftType);

			return swiftType;
		}

		private SwiftType TranslateDictionary(TypeProxy collectionType, TypeProxy keyType, TypeProxy valueType)
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

		private IEnumerable<PropertyProxy> ReadProperties(TypeProxy classType)
		{
			var properties = new List<PropertyProxy>();

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

		private SwiftType TranslateEnum(TypeProxy enumType)
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

			var baseType = enumType.GetEnumUnderlyingType();

			if (baseType != _appDomain.ObjectType)
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

			foreach (var value in enumType.GetEnumValues())
			{
				var member = value.GetMember();
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

					var type = _appDomain.GetDomainType(externalType.fullname);
					var swiftType = new SwiftClass(externalType.swiftname) { IsExcluded = true };

					_swiftTypes.Add(type, swiftType);
				}
			}

			// Add predefined map types.

			foreach (var kvp in _predefinedMapTypes)
			{
				if (!_swiftTypes.ContainsKey(kvp.Key))
					_swiftTypes.Add(kvp.Key, kvp.Value);
			}
		}

		private void AddPredefinedMapType(Type type, SwiftClass swiftClass)
		{
			swiftClass.IsExcluded = true;

			var proxy = _appDomain.GetDomainType(type.AssemblyQualifiedName);

			_predefinedMapTypes.Add(proxy, swiftClass);
		}

		private readonly Poco2SwiftType _configuration;
		private readonly ITypeFilter _filter;
		private readonly DocumentationCache _documentation;
		private readonly IAppDomainProxy _appDomain;
		private readonly IDictionary<TypeProxy, SwiftType> _swiftTypes = new Dictionary<TypeProxy, SwiftType>();
		private readonly IDictionary<string, TypeProxy> _swiftNamesToTypes = new Dictionary<string, TypeProxy>();
		private readonly IDictionary<TypeProxy, string> _swiftTypesToNames = new Dictionary<TypeProxy, string>();
		private readonly IDictionary<TypeProxy, SwiftClass> _predefinedMapTypes = new Dictionary<TypeProxy, SwiftClass>();
	}
}

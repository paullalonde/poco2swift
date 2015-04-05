using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using poco2swift.probe;
using poco2swift.SwiftTypes;
using poco2swift.testdata;

namespace poco2swift.tests
{
	[TestClass]
	public class TranslatorTests
	{
		private IProxyUtils _proxyUtils;
		private IAppDomainProxy _appDomain;
		private SwiftTranslator _translator;
		private SwiftWriter _writer;

		[TestInitialize]
		public void TestInitialize()
		{
			var configuration = new Poco2SwiftType
			{
				imports = new ModuleType[0],
				skiptypes = new SkipType[0],
				externaltypes = new ExternalType[0],
				enumerations = new EnumType[0],
				classes = new ClassType[0],
			};
			var filter = new NullTypeFilter();
			var documentation = new DocumentationCache();
			var callback = new NullProxyCallback();

			_proxyUtils = new NullProxyUtils { Callback = callback };
			_appDomain = new NullAppDomain { Utils = _proxyUtils };
			_translator = new SwiftTranslator(configuration, filter, documentation, _appDomain);
			_writer = new NullWriter(configuration);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			foreach (var tuple in _translator.GetCachedSwiftTypes())
			{
				var swiftType = tuple.Item1;
				var type = tuple.Item2;

				_writer.Write(type, swiftType);
			}

			var swiftSource = _writer.ToString();
		}

		private TypeProxy AddType(Type type)
		{
			var proxy = _proxyUtils.MakeTypeProxy(type);

			_translator.CacheSwiftName(proxy, type.Name);

			return proxy;
		}

		[TestMethod]
		public void NonGenericClass_MakesNonGenericSwiftClass()
		{
			// Arrange
			var expectedType = AddType(typeof(NonGenericTestClass));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNull(swiftClass.BaseType);
			Assert.IsNotNull(swiftClass.TypeParameterNames);
			Assert.IsFalse(swiftClass.TypeParameterNames.Any());
			Assert.IsNotNull(swiftClass.Properties);
			Assert.AreEqual(3, swiftClass.Properties.Count());
			Assert.IsTrue(swiftClass.ContainsProperty("Key"));
			Assert.IsTrue(swiftClass.ContainsProperty("Value"));
			Assert.IsTrue(swiftClass.ContainsProperty("Gizmo"));
		}

		[TestMethod]
		public void GenericClass_MakesGenericSwiftClass()
		{
			// Arrange
			var expectedType = AddType(typeof(GenericTestClass<,>));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNull(swiftClass.BaseType);
			Assert.IsNotNull(swiftClass.TypeParameterNames);
			Assert.AreEqual(2, swiftClass.TypeParameterNames.Count());
			var actualKeyName = swiftClass.TypeParameterNames.Skip(0).First();
			Assert.AreEqual("TKey", actualKeyName);
			var actualValueName = swiftClass.TypeParameterNames.Skip(1).First();
			Assert.AreEqual("TValue", actualValueName);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualKeyName) is SwiftPlaceholder);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualValueName) is SwiftPlaceholder);
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(actualKeyName));
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(actualValueName));
			Assert.IsNotNull(swiftClass.Properties);
			Assert.AreEqual(2, swiftClass.Properties.Count());
			Assert.AreEqual("Key", swiftClass.Properties.First().Name);
			Assert.AreEqual("Value", swiftClass.Properties.Skip(1).First().Name);
		}

		[TestMethod]
		public void NonGenericClass_WithGenericProperty_MakesNonGenericSwiftClass()
		{
			// Arrange
			var expectedType = AddType(typeof(NonGenericClass_WithGenericProperty));

			AddType(typeof(GenericTestClass<,>));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNull(swiftClass.BaseType);
			Assert.IsNotNull(swiftClass.TypeParameterNames);
			Assert.IsFalse(swiftClass.TypeParameterNames.Any());
			Assert.IsNotNull(swiftClass.Properties);
			Assert.AreEqual(1, swiftClass.Properties.Count());
			var property = swiftClass.Properties.First();
			Assert.IsNotNull(property);
			Assert.AreEqual("MyProperty", property.Name);
			var propertyType = property.Type as SwiftOptional;
			Assert.IsNotNull(propertyType);
			var propertyClass = propertyType.InnerType as SwiftClass;
			Assert.IsNotNull(propertyClass);
			Assert.IsNotNull(propertyClass.TypeParameterNames);
			Assert.AreEqual(2, propertyClass.TypeParameterNames.Count());
		}

		[TestMethod]
		public void GenericClass_DerivedFromGenericClass_MakesGenericSwiftClass()
		{
			// Arrange
			var expectedType = AddType(typeof(GenericClass_DerivedFromGenericClass<,>));
			var expectedBaseType = AddType(typeof(GenericTestClass<,>));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNotNull(swiftClass.TypeParameterNames);
			Assert.AreEqual(2, swiftClass.TypeParameterNames.Count());
			var actualKeyName = swiftClass.TypeParameterNames.Skip(0).First();
			Assert.AreEqual("T1", actualKeyName);
			var actualValueName = swiftClass.TypeParameterNames.Skip(1).First();
			Assert.AreEqual("T2", actualValueName);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualKeyName) is SwiftPlaceholder);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualValueName) is SwiftPlaceholder);
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(actualKeyName));
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(actualValueName));
			var swiftBaseClass = swiftClass.BaseType as SwiftClass;
			Assert.IsNotNull(swiftBaseClass);
			Assert.AreEqual(expectedBaseType.Name, swiftBaseClass.Name);
			Assert.IsNotNull(swiftBaseClass.TypeParameterNames);
			Assert.AreEqual(2, swiftBaseClass.TypeParameterNames.Count());
			Assert.AreEqual(actualKeyName, swiftBaseClass.TypeParameterNames.Skip(0).First());
			Assert.AreEqual(actualValueName, swiftBaseClass.TypeParameterNames.Skip(1).First());
			Assert.IsTrue(swiftBaseClass.GetTypeParameter(actualKeyName) is SwiftPlaceholder);
			Assert.IsTrue(swiftBaseClass.GetTypeParameter(actualValueName) is SwiftPlaceholder);
			Assert.IsNull(swiftBaseClass.GetTypeParameterConstraint(actualKeyName));
			Assert.IsNull(swiftBaseClass.GetTypeParameterConstraint(actualValueName));
		}

		[TestMethod]
		public void GenericClass_DerivedFromGenericClassWithDifferentTypeParameters_MakesGenericSwiftClass()
		{
			// Arrange
			var expectedType = AddType(typeof(GenericClass_DerivedFromGenericClassWithDifferentTypeParameters<,,>));
			var expectedBaseType = AddType(typeof(GenericTestClass<,>));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNotNull(swiftClass.TypeParameterNames);
			Assert.AreEqual(3, swiftClass.TypeParameterNames.Count());
			var actualKeyName = swiftClass.TypeParameterNames.Skip(0).First();
			Assert.AreEqual("TKey", actualKeyName);
			var actualT1Name = swiftClass.TypeParameterNames.Skip(1).First();
			Assert.AreEqual("T1", actualT1Name);
			var actualT2Name = swiftClass.TypeParameterNames.Skip(2).First();
			Assert.AreEqual("T2", actualT2Name);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualKeyName) is SwiftPlaceholder);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualT1Name) is SwiftPlaceholder);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualT2Name) is SwiftPlaceholder);
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(actualKeyName));
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(actualT1Name));
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(actualT2Name));
			var swiftBaseClass = swiftClass.BaseType as SwiftClass;
			Assert.IsNotNull(swiftBaseClass);
			Assert.AreEqual(expectedBaseType.Name, swiftBaseClass.Name);
			Assert.IsNotNull(swiftBaseClass.TypeParameterNames);
			Assert.AreEqual(2, swiftBaseClass.TypeParameterNames.Count());
			var actualBaseKeyName = swiftBaseClass.TypeParameterNames.Skip(0).First();
			Assert.AreEqual("TKey", actualBaseKeyName);
			var actualBaseValueName = swiftBaseClass.TypeParameterNames.Skip(1).First();
			Assert.AreEqual("TValue", actualBaseValueName);
			Assert.IsTrue(swiftBaseClass.GetTypeParameter(actualBaseKeyName) is SwiftPlaceholder);
			Assert.IsFalse(swiftBaseClass.GetTypeParameter(actualBaseValueName) is SwiftPlaceholder);
			Assert.IsNull(swiftBaseClass.GetTypeParameterConstraint(actualBaseKeyName));
			Assert.IsNull(swiftBaseClass.GetTypeParameterConstraint(actualBaseValueName));
		}

		[TestMethod]
		public void GenericClass_WithPartiallyGenericProperty_MakesGenericSwiftClass()
		{
			// Arrange
			var expectedType = AddType(typeof(GenericClass_WithPartiallyGenericProperty<>));

			AddType(typeof(GenericTestClass<,>));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNull(swiftClass.BaseType);
			Assert.IsNotNull(swiftClass.TypeParameterNames);
			Assert.AreEqual(1, swiftClass.TypeParameterNames.Count());
			var actualKeyName = swiftClass.TypeParameterNames.First();
			Assert.AreEqual("TKey", actualKeyName);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualKeyName) is SwiftPlaceholder);
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(actualKeyName));
			Assert.IsNotNull(swiftClass.Properties);
			Assert.AreEqual(1, swiftClass.Properties.Count());
			var property = swiftClass.Properties.First();
			Assert.IsNotNull(property);
			Assert.AreEqual("MyProperty", property.Name);
			var propertyType = property.Type as SwiftOptional;
			Assert.IsNotNull(propertyType);
			var propertyClass = propertyType.InnerType as SwiftClass;
			Assert.IsNotNull(propertyClass);
			Assert.IsNotNull(propertyClass.TypeParameterNames);
			Assert.AreEqual(2, propertyClass.TypeParameterNames.Count());
			var actualPropertyKeyName = propertyClass.TypeParameterNames.Skip(0).First();
			Assert.AreEqual("TKey", actualPropertyKeyName);
			var actualPropertyValueName = propertyClass.TypeParameterNames.Skip(1).First();
			Assert.AreEqual("TValue", actualPropertyValueName);
			Assert.IsTrue(propertyClass.GetTypeParameter(actualPropertyKeyName) is SwiftPlaceholder);
			Assert.IsFalse(propertyClass.GetTypeParameter(actualPropertyValueName) is SwiftPlaceholder);
		}

		[TestMethod]
		public void GenericClass_WithConstraint_MakesGenericSwiftClassWithConstraint()
		{
			// Arrange
			var expectedType = AddType(typeof(GenericClass_WithConstraint<>));
			var expectedConstraintType = AddType(typeof(NonGenericTestClass));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNull(swiftClass.BaseType);
			Assert.IsNotNull(swiftClass.TypeParameterNames);
			Assert.AreEqual(1, swiftClass.TypeParameterNames.Count());
			var actualParamName = swiftClass.TypeParameterNames.First();
			Assert.AreEqual("TParam", actualParamName);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualParamName) is SwiftPlaceholder);
			var constraint = swiftClass.GetTypeParameterConstraint(actualParamName) as SwiftClass;
			Assert.IsNotNull(constraint);
			Assert.IsNotNull(swiftClass.Properties);
			Assert.AreEqual(1, swiftClass.Properties.Count());
			var property = swiftClass.Properties.First();
			Assert.IsNotNull(property);
			Assert.AreEqual("MyProperty", property.Name);
			var propertyType = property.Type as SwiftOptional;
			Assert.IsNotNull(propertyType);
			var propertyClass = propertyType.InnerType as SwiftPlaceholder;
			Assert.IsNotNull(propertyClass);
		}

		[TestMethod]
		public void GenericClass_WithStructConstraint_MakesGenericSwiftClassWithoutConstraints()
		{
			// Arrange
			var expectedType = AddType(typeof(GenericClass_WithStructConstraint<>));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNull(swiftClass.BaseType);
			Assert.IsNotNull(swiftClass.TypeParameterNames);
			Assert.AreEqual(1, swiftClass.TypeParameterNames.Count());
			var actualParamName = swiftClass.TypeParameterNames.First();
			Assert.AreEqual("TParam", actualParamName);
			Assert.IsTrue(swiftClass.GetTypeParameter(actualParamName) is SwiftPlaceholder);
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(actualParamName));
			Assert.IsNotNull(swiftClass.Properties);
			Assert.AreEqual(1, swiftClass.Properties.Count());
			var property = swiftClass.Properties.First();
			Assert.IsNotNull(property);
			Assert.AreEqual("MyProperty", property.Name);
			var propertyType = property.Type as SwiftOptional;
			Assert.IsNotNull(propertyType);
			var propertyClass = propertyType.InnerType as SwiftPlaceholder;
			Assert.IsNotNull(propertyClass);
		}

		[TestMethod]
		public void NonGenericClass_DerivedFromGenericClass_MakesFullyConstrainedGenericSwiftClass()
		{
			// Arrange
			var expectedType = AddType(typeof(NonGenericClass_DerivedFromGenericClass));
			var expectedBaseType = AddType(typeof(GenericTestClass<,>));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNotNull(swiftClass.TypeParameterNames);
			Assert.AreEqual(2, swiftClass.TypeParameterNames.Count());
			var actualKeyName = swiftClass.TypeParameterNames.Skip(0).First();
			Assert.AreEqual("TKey", actualKeyName);
			var actualValueName = swiftClass.TypeParameterNames.Skip(1).First();
			Assert.AreEqual("TValue", actualValueName);
			Assert.IsNotNull(swiftClass.GetTypeParameter(actualKeyName) as SwiftPlaceholder);
			Assert.IsNotNull(swiftClass.GetTypeParameter(actualValueName) as SwiftPlaceholder);
			var actualKeyConstraint = swiftClass.GetTypeParameterConstraint(actualKeyName);
			Assert.IsNotNull(actualKeyConstraint);
			//Assert.AreEqual(actualKeyConstraint, AddType(typeof(int)));
			var actualValueConstraint = swiftClass.GetTypeParameterConstraint(actualValueName);
			Assert.IsNotNull(actualValueConstraint);
			//Assert.AreEqual(actualValueConstraint, AddType(typeof(string)));
			var swiftBaseClass = swiftClass.BaseType as SwiftClass;
			Assert.IsNotNull(swiftBaseClass);
			Assert.AreEqual(expectedBaseType.Name, swiftBaseClass.Name);
			Assert.IsNotNull(swiftBaseClass.TypeParameterNames);
			Assert.AreEqual(2, swiftBaseClass.TypeParameterNames.Count());
			var actualBaseKeyName = swiftBaseClass.TypeParameterNames.Skip(0).First();
			Assert.AreEqual("TKey", actualBaseKeyName);
			var actualBaseValueName = swiftBaseClass.TypeParameterNames.Skip(1).First();
			Assert.AreEqual("TValue", actualBaseValueName);
			Assert.IsTrue(swiftBaseClass.GetTypeParameter(actualBaseKeyName) is SwiftPlaceholder);
			Assert.IsFalse(swiftBaseClass.GetTypeParameter(actualBaseValueName) is SwiftPlaceholder);
			Assert.IsNull(swiftBaseClass.GetTypeParameterConstraint(actualBaseKeyName));
			Assert.IsNull(swiftBaseClass.GetTypeParameterConstraint(actualBaseValueName));
		}
	}
}

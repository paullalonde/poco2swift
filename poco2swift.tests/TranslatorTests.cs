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
			Assert.IsNotNull(swiftClass.TypeParameters);
			Assert.IsFalse(swiftClass.TypeParameters.Any());
			Assert.IsNotNull(swiftClass.Properties);
			Assert.AreEqual(2, swiftClass.Properties.Count());
			Assert.IsTrue(swiftClass.ContainsProperty("Key"));
			Assert.IsTrue(swiftClass.ContainsProperty("Value"));
		}

		[TestMethod]
		public void GenericClass_MakesGenericSwiftClass()
		{
			// Arrange
			var expectedType = AddType(typeof(GenericTestClass<,>));
			var keyParameter = new SwiftPlaceholder("TKey");
			var valueParameter = new SwiftPlaceholder("TValue");

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNull(swiftClass.BaseType);
			Assert.IsNotNull(swiftClass.TypeParameters);
			Assert.AreEqual(2, swiftClass.TypeParameters.Count());
			Assert.AreEqual(keyParameter, swiftClass.TypeParameters.First());
			Assert.AreEqual(valueParameter, swiftClass.TypeParameters.Skip(1).First());
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(keyParameter));
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(valueParameter));
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
			var keyParameter = new SwiftPlaceholder("TKey");
			var valueParameter = new SwiftPlaceholder("TValue");

			AddType(typeof(GenericTestClass<,>));

			// Act
			var result = _translator.TranslateType(expectedType);

			// Assert
			Assert.IsNotNull(result);
			var swiftClass = result as SwiftClass;
			Assert.IsNotNull(swiftClass);
			Assert.AreEqual(expectedType.Name, swiftClass.Name);
			Assert.IsNull(swiftClass.BaseType);
			Assert.IsNotNull(swiftClass.TypeParameters);
			Assert.IsFalse(swiftClass.TypeParameters.Any());
			Assert.IsNotNull(swiftClass.Properties);
			Assert.AreEqual(1, swiftClass.Properties.Count());
			var property = swiftClass.Properties.First().Type as SwiftOptional;
			Assert.IsNotNull(property);
			var propertyClass = property.InnerType as SwiftClass;
			Assert.IsNotNull(propertyClass);
			Assert.AreEqual("MyProperty", propertyClass.Name);
			Assert.IsNotNull(propertyClass.TypeParameters);
			Assert.AreEqual(2, swiftClass.TypeParameters.Count());
		}
	}
}

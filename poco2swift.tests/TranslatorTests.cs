using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using poco2swift.SwiftTypes;
using poco2swift.testdata;

namespace poco2swift.tests
{
	[TestClass]
	public class TranslatorTests
	{
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

			_translator = new SwiftTranslator(configuration, filter, documentation);
		}

		private void AddType(Type type)
		{
			_translator.CacheSwiftName(type, type.Name);
		}

		[TestMethod]
		public void NonGenericClass_MakesNonGenericSwiftClass()
		{
			// Arrange
			var expectedType = typeof(NonGenericTestClass);

			AddType(expectedType);

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
			var expectedType = typeof(GenericTestClass<,>);
			var keyParameter = new SwiftPlaceholder("TKey");
			var valueParameter = new SwiftPlaceholder("TValue");

			AddType(expectedType);

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
			Assert.IsTrue(swiftClass.ContainsTypeParameter(keyParameter));
			Assert.IsTrue(swiftClass.ContainsTypeParameter(valueParameter));
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(keyParameter));
			Assert.IsNull(swiftClass.GetTypeParameterConstraint(valueParameter));
			Assert.IsNotNull(swiftClass.Properties);
			Assert.AreEqual(2, swiftClass.Properties.Count());
			Assert.IsTrue(swiftClass.ContainsProperty("Key"));
			Assert.IsTrue(swiftClass.ContainsProperty("Value"));
		}
	}
}

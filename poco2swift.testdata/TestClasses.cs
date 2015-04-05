using System;
using System.Collections.Generic;
using System.Linq;

namespace poco2swift.testdata
{
	public class GenericTestClass<TKey, TValue>
	{
		public TKey Key { get; set; }
		public TValue Value { get; set; }
	}

    public class NonGenericTestClass
    {
		public int Key { get; set; }
		public string Value { get; set; }
		public float Gizmo { get; set; }
	}

	public class NonGenericClass_WithGenericProperty
	{
		public GenericTestClass<int, string> MyProperty { get; set; }
	}

	public class GenericClass_DerivedFromGenericClass<T1, T2> : GenericTestClass<T1, T2>
	{
	}

	public class GenericClass_DerivedFromGenericClassWithDifferentTypeParameters<TKey, T1, T2> : GenericTestClass<TKey, bool>
	{
	}

	public class GenericClass_WithPartiallyGenericProperty<TKey>
	{
		public GenericTestClass<TKey, string> MyProperty { get; set; }
	}

	public class GenericClass_WithConstraint<TParam> where TParam : NonGenericTestClass
	{
		public TParam MyProperty { get; set; }
	}

	public class GenericClass_WithStructConstraint<TParam> where TParam : struct
	{
		public TParam MyProperty { get; set; }
	}

	public class NonGenericClass_DerivedFromGenericClass : GenericTestClass<int, string>
	{
	}
}

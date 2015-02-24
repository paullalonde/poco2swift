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
    }

	public class NonGenericClass_WithGenericProperty
	{
		public GenericTestClass<int, string> MyProperty;
	}
}

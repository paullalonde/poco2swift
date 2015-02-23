using System;
using System.Collections.Generic;
using System.Linq;

namespace poco2swift.testdata
{
	public class GenericTestClass<TKey, TValue>
	{
		TKey Key { get; set; }
		TValue Value { get; set; }
	}

    public class NonGenericTestClass
    {
		int Key { get; set; }
		string Value { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.SwiftTypes
{
	public class SwiftEnumValue
	{
		public SwiftEnumValue(string name, object value)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			if (value == null)
				throw new ArgumentNullException("value");

			_name = name;
			_value = value;
		}

		public string BriefComment { get; set; }

		public void WriteDeclaration(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			SwiftComposite.WriteComment(1, writer, this.BriefComment);
			writer.WriteLine("\tcase {0} = {1}", _name, _value);
		}

		private string _name;
		private object _value;
	}
}

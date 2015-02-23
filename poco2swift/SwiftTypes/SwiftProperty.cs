using System;
using System.IO;

namespace poco2swift.SwiftTypes
{
	class SwiftProperty
	{
		public SwiftProperty(string name, SwiftType type, bool isConstant = false)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			if (type == null)
				throw new ArgumentNullException("type");

			_name = name;
			_type = type;
			_isConstant = isConstant;
		}

		public string BriefComment { get; set; }

		public void WriteDeclaration(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			SwiftComposite.WriteComment(1, writer, this.BriefComment);
			writer.Write("\t{0} {1}: ", _isConstant ? "let" : "var", _name);
			_type.Write(writer);
			writer.WriteLine();
		}

		private string _name;
		private SwiftType _type;
		private bool _isConstant;
	}
}

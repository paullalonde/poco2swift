using System;
using System.IO;

namespace poco2swift.SwiftTypes
{
	public class SwiftProperty
	{
		public SwiftProperty(string name, SwiftType type, bool isConstant = false)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			if (type == null)
				throw new ArgumentNullException("type");

			this.Name = name;
			this.Type = type;

			_isConstant = isConstant;
		}

		public string Name { get; private set; }
		public SwiftType Type { get; private set; }
		public string BriefComment { get; set; }

		public void WriteDeclaration(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			SwiftComposite.WriteComment(1, writer, this.BriefComment);
			writer.Write("\t{0} {1}: ", _isConstant ? "let" : "var", this.Name);
			this.Type.Write(writer);
			writer.WriteLine();
		}

		private bool _isConstant;
	}
}

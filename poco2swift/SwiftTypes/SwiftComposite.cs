using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace poco2swift.SwiftTypes
{
	public abstract class SwiftComposite : SwiftType
	{
		public SwiftComposite(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			this.Name = name;
		}

		public string Name { get; private set; }
		public string BriefComment { get; set; }
		public SwiftType BaseType { get; set; }

		protected abstract void WriteKeyword(SwiftWriter writer);
		protected abstract void WriteDeclaration(SwiftWriter writer);
		protected abstract void WriteChildren(SwiftWriter writer);

		public void WriteDefinition(SwiftWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			WriteComment(0, writer, this.BriefComment);
			WriteKeyword(writer);
			writer.Write(" ");
			WriteDeclaration(writer);

			if (this.BaseType != null)
			{
				writer.Write(": ");
				this.BaseType.Write(writer);
			}

			writer.WriteLine(" {");
			WriteChildren(writer);
			writer.WriteLine("}");
		}

		#region Object overrides

		public override string ToString()
		{
			return this.Name;
		}

		#endregion

		internal static bool WriteComment(int indent, SwiftWriter writer, string comment)
		{
			bool wroteComment = false;

			if (!String.IsNullOrWhiteSpace(comment))
			{
				var lines = comment.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

				foreach (var line in lines)
				{
					writer.WriteLine("{0}/// {1}", new string('\t', indent), line.Trim());
					wroteComment = true;
				}
			}

			return wroteComment;
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace poco2swift.SwiftTypes
{
	abstract class SwiftComposite : SwiftType
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

		protected abstract string SourceDeclaration { get; }

		protected abstract void WriteChildren(TextWriter writer);

		public void WriteDeclaration(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			WriteComment(0, writer, this.BriefComment);
			writer.Write("{0} ", this.SourceDeclaration);
			this.Write(writer);

			if (this.BaseType != null)
			{
				writer.Write(": ");
				this.BaseType.Write(writer);
			}

			writer.WriteLine(" {");
			WriteChildren(writer);
			writer.WriteLine("}");
		}

		internal static bool WriteComment(int indent, TextWriter writer, string comment)
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

		#region Type parameters

		public IEnumerable<SwiftPlaceholder> TypeParameters
		{
			get { return _typeParameters; }
		}

		public void AddTypeParameter(SwiftPlaceholder parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");

			_typeParameters.Add(parameter);
		}

		#endregion

		#region SwiftType overrides

		public override void Write(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(this.Name);

			if (_typeParameters.Count > 0)
			{
				writer.Write("<{0}>", String.Join(", ", from tp in _typeParameters select tp.Name));
			}
		}

		#endregion

		private IList<SwiftPlaceholder> _typeParameters = new List<SwiftPlaceholder>();
	}
}

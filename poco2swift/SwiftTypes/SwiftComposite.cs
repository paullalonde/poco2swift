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

		#region Type parameters

		public IEnumerable<string> TypeParameterNames
		{
			get { return _parameterNames; }
		}

		public void AddTypeParameter(SwiftPlaceholder placeholder)
		{
			if (placeholder == null)
				throw new ArgumentNullException("placeholder");

			AddTypeParameter(placeholder.Name, placeholder);
		}

		public void AddTypeParameter(string name, SwiftType parameter)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			if (parameter == null)
				throw new ArgumentNullException("parameter");

			if (_parameters.ContainsKey(name))
				throw new ArgumentOutOfRangeException("name", name, "Duplicate type parameter name.");

			_parameters.Add(name, parameter);
			_parameterNames.Add(name);
		}

		public SwiftType GetTypeParameter(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			SwiftType parameter;

			if (!_parameters.TryGetValue(name, out parameter))
				throw new ArgumentOutOfRangeException("name", name, "Unknown type parameter name.");

			return parameter;
		}

		public void AddTypeParameterContraint(string name, SwiftType constraint)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			if (constraint == null)
				throw new ArgumentNullException("constraint");

			if (!_parameters.ContainsKey(name))
				throw new ArgumentOutOfRangeException("name", name, "Unknown type parameter name.");

			_parameterContraints.Add(name, constraint);
		}

		public SwiftType GetTypeParameterConstraint(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			SwiftType parameter;

			if (!_parameterContraints.TryGetValue(name, out parameter))
				parameter = null;

			return parameter;
		}

		#endregion

		protected abstract void WriteKeyword(SwiftWriter writer);
		protected abstract void WriteChildren(SwiftWriter writer);

		public void WriteDefinition(SwiftWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			WriteComment(0, writer, this.BriefComment);
			WriteKeyword(writer);
			writer.Write(" ");
			WriteDeclaration(writer, true);

			if (this.BaseType != null)
			{
				writer.Write(": ");
				this.BaseType.Write(writer);
			}

			writer.WriteLine(" {");
			WriteChildren(writer);
			writer.WriteLine("}");
		}

		#region SwiftType overrides

		public override void Write(SwiftWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			WriteDeclaration(writer, false);
		}

		#endregion

		#region Object overrides

		public override string ToString()
		{
			return this.Name;
		}

		#endregion

		protected void WriteDeclaration(SwiftWriter writer, bool forDefinition)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(this.Name);

			if (this.TypeParameterNames.Any())
			{
				var separator = "";

				writer.Write("<");

				foreach (var name in this.TypeParameterNames)
				{
					writer.Write(separator);

					var parameter = GetTypeParameter(name);

					parameter.Write(writer);

					if (forDefinition)
					{
						var constraint = GetTypeParameterConstraint(name);

						if (constraint != null)
						{
							writer.Write(": ");
							constraint.Write(writer);
						}
					}

					separator = ", ";
				}

				writer.Write(">");
			}
		}

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

		private IList<string> _parameterNames = new List<string>();
		private IDictionary<string, SwiftType> _parameters = new Dictionary<string, SwiftType>();
		private IDictionary<string, SwiftType> _parameterContraints = new Dictionary<string, SwiftType>();
	}
}

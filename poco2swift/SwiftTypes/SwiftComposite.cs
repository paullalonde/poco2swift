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
			get { return _orderedParameters; }
		}

		public bool ContainsTypeParameter(SwiftPlaceholder parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");

			return _parameters.Contains(parameter);
		}

		public void AddTypeParameter(SwiftPlaceholder parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");

			if (_parameters.Contains(parameter))
				throw new ArgumentException(String.Format("Duplicate parameter : {0}.", parameter.Name));

			_parameters.Add(parameter);
			_orderedParameters.Add(parameter);
		}

		public void AddTypeParameterContraint(SwiftPlaceholder parameter, SwiftType constraint)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");

			if (constraint == null)
				throw new ArgumentNullException("constraint");

			if (!_parameters.Contains(parameter))
				throw new ArgumentException(String.Format("Unknown parameter : {0}.", parameter.Name));

			_parameterContraints.Add(parameter, constraint);
		}

		public SwiftType GetTypeParameterConstraint(SwiftPlaceholder parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");

			SwiftType constraint;

			if (!_parameterContraints.TryGetValue(parameter, out constraint))
				constraint = null;

			return constraint;
		}

		#endregion

		#region SwiftType overrides

		public override void Write(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(this.Name);

			if (_orderedParameters.Any())
			{
				var separator = "";

				writer.Write("<");

				foreach (var parameter in _orderedParameters)
				{
					writer.Write(separator);
					writer.Write(parameter.Name);

					SwiftType constraint;

					if (_parameterContraints.TryGetValue(parameter, out constraint))
					{
						writer.Write(": ");
						constraint.Write(writer);
					}
				}

				writer.Write(">");
			}
		}

		#endregion

		private ISet<SwiftPlaceholder> _parameters = new HashSet<SwiftPlaceholder>();
		private IList<SwiftPlaceholder> _orderedParameters = new List<SwiftPlaceholder>();
		private IDictionary<SwiftPlaceholder, SwiftType> _parameterContraints = new Dictionary<SwiftPlaceholder, SwiftType>();
	}
}

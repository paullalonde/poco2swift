using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.SwiftTypes
{
	public class SwiftReference : SwiftType
	{
		public SwiftReference(SwiftComposite definition)
		{
			if (definition == null)
				throw new ArgumentNullException("definition");

			_definition = definition;
		}

		public void ResolveTypeParameter(SwiftPlaceholder name, SwiftType resolvedType)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			if (resolvedType == null)
				throw new ArgumentNullException("resolvedType");

			if (!_definition.ContainsTypeParameter(name))
				throw new ArgumentException(String.Format("Unknown parameter : {0}.", name.Name));
			
			_resolvedParameters.Add(name, resolvedType);
		}

		#region SwiftType overrides

		public override void Write(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(_definition.Name);

			var definitionParameters = _definition.TypeParameters;

			if (definitionParameters.Any())
			{
				var separator = "";

				writer.Write("<");

				foreach (var parameter in definitionParameters)
				{
					writer.Write(separator);

					SwiftType resolvedType;

					if (_resolvedParameters.TryGetValue(parameter, out resolvedType))
						resolvedType.Write(writer);
					else
						writer.Write(parameter.Name);

					separator = ", ";
				}

				writer.Write(">");
			}
		}

		#endregion

		private SwiftComposite _definition;
		private IDictionary<SwiftPlaceholder, SwiftType> _resolvedParameters = new Dictionary<SwiftPlaceholder, SwiftType>();
	}
}

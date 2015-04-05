using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace poco2swift.SwiftTypes
{
	public class SwiftClass : SwiftComposite
	{
		public SwiftClass(string name)
			: base(name)
		{
		}

		#region Properties

		public IEnumerable<SwiftProperty> Properties
		{
			get { return _orderedProperties; }
		}

		public bool ContainsProperty(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			return _properties.ContainsKey(name);
		}

		public void AddProperty(SwiftProperty property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			var name = property.Name;

			if (_properties.ContainsKey(name))
				throw new ArgumentException(String.Format("Duplicate property : {0}.", name));

			_properties.Add(name, property);
			_orderedProperties.Add(property);
		}

		#endregion

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

		#region SwiftComposite overrides

		protected override void WriteKeyword(SwiftWriter writer)
		{
			writer.Write("class");
		}

		protected override void WriteDeclaration(SwiftWriter writer)
		{
			WriteDeclaration(writer, true);
		}

		protected override void WriteChildren(SwiftWriter writer)
		{
			var firstTime = true;

			foreach (var property in _orderedProperties)
			{
				if (!firstTime)
					writer.WriteLine("\t");

				property.WriteDeclaration(writer);

				firstTime = false;
			}
		}

		#endregion

		#region SwiftType overrides

		public override void Write(SwiftWriter writer)
		{
			WriteDeclaration(writer, false);
		}

		#endregion

		private void WriteDeclaration(SwiftWriter writer, bool forDefinition)
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

		private IDictionary<string, SwiftProperty> _properties = new Dictionary<string, SwiftProperty>();
		private IList<SwiftProperty> _orderedProperties = new List<SwiftProperty>();
		private IList<string> _parameterNames = new List<string>();
		private IDictionary<string, SwiftType> _parameters = new Dictionary<string, SwiftType>();
		private IDictionary<string, SwiftType> _parameterContraints = new Dictionary<string, SwiftType>();
	}
}

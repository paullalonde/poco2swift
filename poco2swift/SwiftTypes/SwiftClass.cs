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

		#region SwiftComposite overrides

		protected override void WriteKeyword(SwiftWriter writer)
		{
			writer.Write("class");
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

		private IDictionary<string, SwiftProperty> _properties = new Dictionary<string, SwiftProperty>();
		private IList<SwiftProperty> _orderedProperties = new List<SwiftProperty>();
	}
}

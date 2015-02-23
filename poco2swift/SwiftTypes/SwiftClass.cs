using System;
using System.Collections.Generic;
using System.IO;

namespace poco2swift.SwiftTypes
{
	class SwiftClass : SwiftComposite
	{
		public SwiftClass(string name)
			: base(name)
		{
		}

		#region Properties

		public void AddProperty(SwiftProperty property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			_properties.Add(property);
		}

		#endregion

		#region SwiftComposite overrides

		protected override string SourceDeclaration { get { return "class"; } }

		protected override void WriteChildren(TextWriter writer)
		{
			var firstTime = true;

			foreach (var property in _properties)
			{
				if (!firstTime)
					writer.WriteLine("\t");

				property.WriteDeclaration(writer);

				firstTime = false;
			}
		}

		#endregion

		private IList<SwiftProperty> _properties = new List<SwiftProperty>();
	}
}

using System;
using System.Collections.Generic;
using System.IO;

namespace poco2swift.SwiftTypes
{
	public class SwiftEnum : SwiftComposite
	{
		public SwiftEnum(string name)
			: base(name)
		{
		}

		#region Values

		public void AddValue(SwiftEnumValue value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			_values.Add(value);
		}

		#endregion

		#region SwiftComposite overrides

		protected override string SourceDeclaration { get { return "enum"; } }

		protected override void WriteChildren(TextWriter writer)
		{
			var firstTime = true;
			var previousValueHadDoc = false;

			foreach (var value in _values)
			{
				var hasDoc = !String.IsNullOrWhiteSpace(value.BriefComment);

				if (!firstTime)
				{
					if (hasDoc)
						writer.WriteLine("\t");
					else if (previousValueHadDoc)
						writer.WriteLine("\t");
				}

				value.WriteDeclaration(writer);

				firstTime = false;
				previousValueHadDoc = hasDoc;
			}
		}

		#endregion

		private IList<SwiftEnumValue> _values = new List<SwiftEnumValue>();
	}
}

using System;
using System.Collections.Generic;

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

		protected override void WriteKeyword(SwiftWriter writer)
		{
			writer.Write("enum");
		}

		protected override void WriteDeclaration(SwiftWriter writer)
		{
			Write(writer);
		}

		protected override void WriteChildren(SwiftWriter writer)
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

		#region SwiftType overrides

		public override void Write(SwiftWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(this.Name);
		}

		#endregion

		private IList<SwiftEnumValue> _values = new List<SwiftEnumValue>();
	}
}

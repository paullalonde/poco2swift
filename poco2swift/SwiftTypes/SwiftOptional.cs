using System;
using System.IO;

namespace poco2swift.SwiftTypes
{
	public class SwiftOptional : SwiftType
	{
		public SwiftOptional(SwiftType innerType, bool implicitlyUnwrapped = false)
		{
			if (innerType == null)
				throw new ArgumentNullException("innerType");

			this.InnerType = innerType;
			
			_implicitlyUnwrapped = implicitlyUnwrapped;
		}

		public SwiftType InnerType { get; private set; }

		#region SwiftType overrides

		public override bool IsOptional { get { return true; } }

		public override void Write(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			this.InnerType.Write(writer);
			writer.Write(_implicitlyUnwrapped ? "!" : "?");
		}

		#endregion

		private bool _implicitlyUnwrapped;
	}
}

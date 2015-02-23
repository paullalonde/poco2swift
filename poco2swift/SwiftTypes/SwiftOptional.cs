using System;
using System.IO;

namespace poco2swift.SwiftTypes
{
	class SwiftOptional : SwiftType
	{
		public SwiftOptional(SwiftType innerType, bool implicitlyUnwrapped = false)
		{
			if (innerType == null)
				throw new ArgumentNullException("innerType");

			_innerType = innerType;
			_implicitlyUnwrapped = implicitlyUnwrapped;
		}

		#region SwiftType overrides

		public override bool IsOptional { get { return true; } }

		public override void Write(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			_innerType.Write(writer);
			writer.Write(_implicitlyUnwrapped ? "!" : "?");
		}

		#endregion

		private SwiftType _innerType;
		private bool _implicitlyUnwrapped;
	}
}

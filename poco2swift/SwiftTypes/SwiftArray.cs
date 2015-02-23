using System;
using System.IO;

namespace poco2swift.SwiftTypes
{
	public class SwiftArray : SwiftType
	{
		public SwiftArray(SwiftType elementType)
		{
			if (elementType == null)
				throw new ArgumentNullException("elementType");

			_elementType = elementType;
		}

		#region SwiftType overrides

		public override void Write(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write("[");
			_elementType.Write(writer);
			writer.Write("]");
		}

		#endregion

		private SwiftType _elementType;
	}
}

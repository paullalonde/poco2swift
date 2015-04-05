using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.SwiftTypes
{
	public class SwiftSet : SwiftType
	{
		public SwiftSet(SwiftType elementType)
		{
			if (elementType == null)
				throw new ArgumentNullException("elementType");

			_elementType = elementType;

			base.IsValueType = true;
		}

		#region SwiftType overrides

		public override void Write(SwiftWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(writer.IsEmittingSwift_12 ? "Set<" : "[");
			_elementType.Write(writer);
			writer.Write(writer.IsEmittingSwift_12 ? ">" : "]");
		}

		#endregion

		private SwiftType _elementType;
	}
}

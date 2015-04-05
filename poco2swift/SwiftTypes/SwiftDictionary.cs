using System;

namespace poco2swift.SwiftTypes
{
	public class SwiftDictionary : SwiftType
	{
		public SwiftDictionary(SwiftType keyType, SwiftType valueType)
		{
			if (keyType == null)
				throw new ArgumentNullException("keyType");

			if (valueType == null)
				throw new ArgumentNullException("valueType");

			_keyType = keyType;
			_valueType = valueType;

			base.IsValueType = true;
		}

		#region SwiftType overrides

		public override void Write(SwiftWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write("[");
			_keyType.Write(writer);
			writer.Write(": ");
			_valueType.Write(writer);
			writer.Write("]");
		}

		#endregion

		private SwiftType _keyType;
		private SwiftType _valueType;
	}
}

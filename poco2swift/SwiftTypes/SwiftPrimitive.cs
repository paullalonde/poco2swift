using System;
using System.Collections.Generic;
using System.IO;

namespace Pod2Swift.SwiftTypes
{
	class SwiftPrimitive : SwiftType
	{
		public static SwiftPrimitive FromType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			SwiftPrimitive primitiveType;

			if (!_PrimitiveTypes.TryGetValue(type, out primitiveType))
				primitiveType = null;

			return primitiveType;
		}

		private SwiftPrimitive(TypeCode typeCode)
		{
			_typeCode = typeCode;
		}

		#region SwiftType overrides

		public override bool IsValueType { get { return true; } }

		public override void Write(TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(_typeCode.ToString());
		}

		#endregion

		private TypeCode _typeCode;

		private enum TypeCode
		{
			Bool,
			String,
			Int8,
			Int16,
			Int32,
			Int64,
			UInt8,
			UInt16,
			UInt32,
			UInt64,
			Float,
			Double,
			NSUUID,
			NSURL,
		}

		private static readonly IDictionary<Type, SwiftPrimitive> _PrimitiveTypes = new Dictionary<Type, SwiftPrimitive>
		{
			{ typeof(SByte),  new SwiftPrimitive(TypeCode.Int8)   },
			{ typeof(Int16),  new SwiftPrimitive(TypeCode.Int16)  },
			{ typeof(Int32),  new SwiftPrimitive(TypeCode.Int32)  },
			{ typeof(Int64),  new SwiftPrimitive(TypeCode.Int64)  },
			{ typeof(Byte),   new SwiftPrimitive(TypeCode.UInt8)  },
			{ typeof(UInt16), new SwiftPrimitive(TypeCode.UInt16) },
			{ typeof(UInt32), new SwiftPrimitive(TypeCode.UInt32) },
			{ typeof(UInt64), new SwiftPrimitive(TypeCode.UInt64) },
			{ typeof(string), new SwiftPrimitive(TypeCode.String) },
			{ typeof(bool),   new SwiftPrimitive(TypeCode.Bool)   },
			{ typeof(float),  new SwiftPrimitive(TypeCode.Float)  },
			{ typeof(double), new SwiftPrimitive(TypeCode.Double) },
			{ typeof(Guid),   new SwiftPrimitive(TypeCode.NSUUID) },
			{ typeof(Uri),    new SwiftPrimitive(TypeCode.NSURL)  },
		};
	}
}

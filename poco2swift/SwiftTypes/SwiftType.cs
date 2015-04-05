using System;

namespace poco2swift.SwiftTypes
{
	public abstract class SwiftType
	{
		public virtual bool IsOptional { get { return false; } }

		public bool IsExcluded { get; set; }

		public abstract void Write(SwiftWriter writer);
	}
}

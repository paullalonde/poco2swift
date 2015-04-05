using System;

namespace poco2swift.SwiftTypes
{
	/// <summary>
	/// Base class for Swift types.
	/// </summary>
	public abstract class SwiftType
	{
		/// <summary>
		/// Is this an optional?
		/// </summary>
		public virtual bool IsOptional { get { return false; } }

		/// <summary>
		/// Is this a value type (ie struct or enum) ?
		/// </summary>
		public bool IsValueType { get; set; }

		/// <summary>
		/// Should this type be excluded, i.e. not emitted into the Swift source code?
		/// </summary>
		public bool IsExcluded { get; set; }

		/// <summary>
		/// Emit the type's declaration, as used in property definitions.
		/// </summary>
		/// <param name="writer"></param>
		public abstract void Write(SwiftWriter writer);
	}
}

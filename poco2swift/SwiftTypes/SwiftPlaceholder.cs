﻿using System;

namespace poco2swift.SwiftTypes
{
	public class SwiftPlaceholder : SwiftType, IEquatable<SwiftPlaceholder>, IComparable<SwiftPlaceholder>
	{
		public SwiftPlaceholder(string name)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			this.Name = name;
		}

		public string Name { get; private set; }

		#region SwiftType overrides

		public override void Write(SwiftWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.Write(this.Name);
		}

		#endregion

		#region Object overrides

		public override bool Equals(object obj)
		{
			return this.Equals(obj as SwiftPlaceholder);
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		#endregion

		#region IEquatable<SwiftPlaceholder> implementation

		public bool Equals(SwiftPlaceholder other)
		{
			if (other == null)
				return false;
			else if (Object.ReferenceEquals(this, other))
				return true;
			else
				return this.Name.Equals(other.Name);
		}

		#endregion

		#region IEquatable<SwiftPlaceholder> implementation

		public int CompareTo(SwiftPlaceholder other)
		{
			return this.Name.CompareTo(other.Name);
		}

		#endregion
	}
}

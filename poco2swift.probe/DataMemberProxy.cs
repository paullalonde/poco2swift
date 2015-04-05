using System;

namespace poco2swift.probe
{
	public class DataMemberProxy : MarshalByRefObject
	{
		public bool EmitDefaultValue { get; set; }
		public bool IsRequired { get; set; }
		public string Name { get; set; }
		public int Order { get; set; }

		#region MarshalByRefObject overrides

		public override object InitializeLifetimeService()
		{
			return null;
		}

		#endregion
	}
}

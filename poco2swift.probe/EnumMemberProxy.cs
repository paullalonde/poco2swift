using System;

namespace poco2swift.probe
{
	public class EnumMemberProxy : MarshalByRefObject
	{
		public string Value { get; set; }

		#region MarshalByRefObject overrides

		public override object InitializeLifetimeService()
		{
			return null;
		}

		#endregion
	}
}

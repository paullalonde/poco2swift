using System;

namespace poco2swift.probe
{
	public class DataContractProxy : MarshalByRefObject
	{
		public bool IsReference { get; set; }
		public string Name { get; set; }
		public string Namespace { get; set; }

		#region MarshalByRefObject overrides

		public override object InitializeLifetimeService()
		{
			return null;
		}

		#endregion
	}
}

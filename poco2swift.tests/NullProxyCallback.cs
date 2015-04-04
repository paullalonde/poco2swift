using System;
using poco2swift.probe;

namespace poco2swift.tests
{
	class NullProxyCallback : IProxyCallback
	{
		#region IProxyCallback implementation

		public void WriteError(string message)
		{
			Console.Error.WriteLine(" {0}", message);
		}

		#endregion
	}
}

using System;
using poco2swift.probe;

namespace poco2swift
{
	public class ProxyCallbackImpl : MarshalByRefObject, IProxyCallback
	{
		#region IProbeCallback implementation

		public void WriteError(string message)
		{
			ErrorHandler.Error("Target Domain : {0}", message);
		}

		#endregion
	}
}

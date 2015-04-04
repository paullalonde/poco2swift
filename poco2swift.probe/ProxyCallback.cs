using System;
using System.Globalization;

namespace poco2swift.probe
{
	internal class ProxyCallback : IProxyCallback
	{
		//public ProxyCallback(AppDomain hostDomain, Action<string> errorHandler)
		//{
		//	//if (hostDomain == null)
		//	//	throw new ArgumentNullException("hostDomain");

		//	if (errorHandler == null)
		//		throw new ArgumentNullException("errorHandler");

		//	//_hostDomain = hostDomain;
		//	_errorHandler = errorHandler;
		//}

		#region IProbeCallback implementation

		public void WriteError(string message)
		{
			Console.Error.WriteLine("Target Domain : {0}", message);
		}

		#endregion

		//private readonly AppDomain _hostDomain;
		//private Action<string> _errorHandler;
	}
}

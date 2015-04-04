using System;

namespace poco2swift.probe
{
	public interface IProxyCallback
	{
		void WriteError(string message);
	}
}

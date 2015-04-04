using System;
using System.Globalization;

namespace poco2swift.probe
{
	internal static class ProbeExtensions
	{
		public static void WriteError(this IProxyCallback callback, string message, params object[] args)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			callback.WriteError(String.Format(CultureInfo.CurrentCulture, "Cannot resolve module '{0}' from assembly '{1}'", args));
		}
	}
}

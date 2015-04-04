using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift
{
	static class ErrorHandler
	{
		public const int DUPLICATE_TYPE_NAME = 1;
		public const int MISSING_FULL_NAME = 2;
		public const int UNDEFINED_CLASS = 3;
		public const int UNDEFINED_ENUM = 4;
		public const int UNRESOLVED_ASSEMBLY = 5;

		public static void Error(string message, params object[] args)
		{
			Console.Error.WriteLine(message, args);
		}

		public static void Fatal(int exitCode, string message, params object[] args)
		{
			Console.Error.WriteLine(message, args);

			Environment.Exit(exitCode);
		}
	}
}

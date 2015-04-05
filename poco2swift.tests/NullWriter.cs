using System.IO;
using poco2swift.probe;

namespace poco2swift.tests
{
	class NullWriter : SwiftWriter
	{
		public NullWriter(Poco2SwiftType configuration)
			: base(configuration)
		{
			_writer = new StringWriter();

			WriteFilePreamble(_writer);
		}

		#region SwiftWriter overrides

		protected override TextWriter GetWriter(TypeProxy type)
		{
			return _writer;
		}

		#endregion

		public override string ToString()
		{
			_writer.Flush();

			return _writer.ToString();
		}

		private readonly StringWriter _writer;
	}
}

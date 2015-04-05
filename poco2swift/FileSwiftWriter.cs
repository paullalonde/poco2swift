using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using poco2swift.probe;
using poco2swift.SwiftTypes;

namespace poco2swift
{
	class FileSwiftWriter : SwiftWriter, IDisposable
	{
		public FileSwiftWriter(Poco2SwiftType configuration, bool swift_12, string outputDir)
			: base(configuration, swift_12)
		{
			if (String.IsNullOrEmpty(outputDir))
				throw new ArgumentNullException("outputDir");

			_outputDir = outputDir;
		}

		protected override TextWriter GetWriter(TypeProxy type)
		{
			var assembly = type.Assembly;
			TextWriter writer;

			if (!_writers.TryGetValue(assembly, out writer))
			{
				var assemblyPath = assembly.Location;
				var assemblyName = Path.GetFileName(assemblyPath);
				var swiftName = Path.ChangeExtension(assemblyName, ".swift");
				var swiftPath = Path.Combine(_outputDir, swiftName);

				writer = File.CreateText(swiftPath);

				_writers.Add(assembly, writer);

				WriteFilePreamble(writer);
			}

			return writer;
		}

		#region IDisposable implementation

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_writers != null)
				{
					foreach (var writer in _writers.Values)
						writer.Dispose();

					_writers = null;
				}

				_disposed = true;
			}
		}

		protected void CheckDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException("InMemoryBlobContext");
		}

		#endregion

		private readonly string _outputDir;
		private bool _disposed;
		private IDictionary<AssemblyProxy, TextWriter> _writers = new Dictionary<AssemblyProxy, TextWriter>();
	}
}

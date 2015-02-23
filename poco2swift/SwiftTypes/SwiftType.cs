using System.Globalization;
using System.IO;
using System.Text;

namespace poco2swift.SwiftTypes
{
	public abstract class SwiftType
	{
		public virtual bool IsOptional { get { return false; } }

		public abstract void Write(TextWriter writer);

		public string Declaration
		{
			get
			{
				using (var writer = new StringWriter(CultureInfo.InvariantCulture))
				{
					Write(writer);

					writer.Flush();

					return writer.ToString();
				}
			}
		}
	}
}

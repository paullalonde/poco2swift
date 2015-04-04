using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.probe
{
	public class DataMemberProxy : MarshalByRefObject
	{
		public bool EmitDefaultValue { get; set; }
		public bool IsRequired { get; set; }
		public string Name { get; set; }
		public int Order { get; set; }
	}
}

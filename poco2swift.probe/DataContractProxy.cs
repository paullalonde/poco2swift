using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace poco2swift.probe
{
	public class DataContractProxy : MarshalByRefObject
	{
		public bool IsReference { get; set; }
		public string Name { get; set; }
		public string Namespace { get; set; }
	}
}

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Yieldly.V1.Asc {

	internal class ProgramLogic {

		[JsonProperty("bytecode")]
		public string ByteCode { get; set; }

		[JsonProperty("address")]
		public string Address { get; set; }

		[JsonProperty("size")]
		public int Size { get; set; }

		[JsonProperty("variables")]
		public List<Variable> Variables { get; set; }

		[JsonProperty("source")]
		public string Source { get; set; }

	}

}

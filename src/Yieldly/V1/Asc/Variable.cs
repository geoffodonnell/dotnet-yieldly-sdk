using Newtonsoft.Json;

namespace Yieldly.V1.Asc {

	internal class Variable {

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("index")]
		public int Index { get; set; }

		[JsonProperty("length")]
		public int Length { get; set; }

	}

}

using Newtonsoft.Json;

namespace Yieldly.V1.Asc {

	[ContractType(TypeKeyValue)]
	internal class AppContract : Contract {

		public const string TypeKeyValue = "app";

		[JsonProperty("approval_program")]
		public ProgramLogic ApprovalProgram { get; set; }

		[JsonProperty("clear_program")]
		public ProgramLogic ClearProgram { get; set; }

		[JsonProperty("global_state_schema")]
		public Schema GlobalStateSchema { get; set; }

		[JsonProperty("local_state_schema")]
		public Schema LocalStateSchema { get; set; }

		public AppContract() {
			Type = TypeKeyValue;
		}

	}

}

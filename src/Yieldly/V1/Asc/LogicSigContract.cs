using Newtonsoft.Json;

namespace Yieldly.V1.Asc {

	[ContractType(TypeKeyValue)]
	internal class LogicSigContract : Contract {

		public const string TypeKeyValue = "logicsig";

		[JsonProperty("logic")]
		public ProgramLogic Logic { get; set; }

		public LogicSigContract() {
			Type = TypeKeyValue;
		}

	}

}

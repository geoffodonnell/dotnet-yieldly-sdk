using System;

namespace Yieldly.V1.Asc {

	public sealed class ContractTypeAttribute : Attribute {

		public string Type { get; set; }

		public ContractTypeAttribute(string type) {
			Type = type;
		}

	}

}

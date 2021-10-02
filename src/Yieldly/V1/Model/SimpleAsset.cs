using System;

namespace Yieldly.V1.Model {

	public class SimpleAsset {

		/// <summary>
		/// Asset ID of the holding
		/// </summary>
		public ulong Id { get; set; }

		/// <summary>
		/// Name of this asset, as supplied by the creator
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Name of a unit of this asset, as supplied by the creator.
		/// </summary>
		public string UnitName { get; set; }

		/// <summary>
		/// This value must be between 0 and 19 (inclusive)
		/// </summary>
		public int Decimals { get; set; }

		public virtual double ToDisplayValue(ulong baseValue) {
			return baseValue / Math.Pow(10, Decimals);
		}

	}

}

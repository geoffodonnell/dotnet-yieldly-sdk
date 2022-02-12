using System;

namespace Yieldly.V1.Model {

	public class SimpleAsset {

		/// <summary>
		/// Asset ID of the holding
		/// </summary>
		public virtual ulong Id { get; set; }

		/// <summary>
		/// Name of this asset, as supplied by the creator
		/// </summary>
		public virtual string Name { get; set; }

		/// <summary>
		/// Name of a unit of this asset, as supplied by the creator.
		/// </summary>
		public virtual string UnitName { get; set; }

		/// <summary>
		/// This value must be between 0 and 19 (inclusive)
		/// </summary>
		public virtual int Decimals { get; set; }

		/// <summary>
		/// Creator address
		/// </summary>
		public virtual string Creator { get; set; }

		public virtual double ToDisplayValue(ulong baseValue) {
			return baseValue / Math.Pow(10, Decimals);
		}

	}

}

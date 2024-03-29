﻿using Algorand.Algod.Model;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Linq;
using Yieldly.V1.Model;

namespace Yieldly.V1 {

	public static class YieldlyExtensions {

		private static readonly StringComparison mCmp = StringComparison.Ordinal;

		private static readonly string GlobalTimeKey
			= Base64.ToBase64String(Strings.ToUtf8ByteArray("GT"));

		private static readonly string GlobalStakingSharesKey
			= Base64.ToBase64String(Strings.ToUtf8ByteArray("GSS"));

		private static readonly string TotalClaimableYieldlyKey
			= Base64.ToBase64String(Strings.ToUtf8ByteArray("TYUL"));

		private static readonly string TotalClaimableAsaKey
			= Base64.ToBase64String(Strings.ToUtf8ByteArray("TYUL"));

		private static readonly string TotalClaimableAlgoKey
			= Base64.ToBase64String(Strings.ToUtf8ByteArray("TAP"));

		private static readonly string UserStakingSharesKey
			= Base64.ToBase64String(Strings.ToUtf8ByteArray("USS"));

		private static readonly string UserAmountKey
			= Base64.ToBase64String(Strings.ToUtf8ByteArray("UA"));

		private static readonly string UserTimeKey
			= Base64.ToBase64String(Strings.ToUtf8ByteArray("UT"));

		public static SimpleAsset ToSimpleAsset(this Asset asset) {

			return new SimpleAsset {
				Id = (ulong)asset.Index,
				Name = asset.Params.Name,
				UnitName = asset.Params.UnitName,
				Decimals = (int)asset.Params.Decimals
			};
		}
		
		public static ulong? GetGlobalTimeValue(this ICollection<TealKeyValue> values) {
			return values
				.FirstOrDefault(s => String.Equals(s.Key, GlobalTimeKey, mCmp))?
				.Value?
				.Uint;
		}

		public static ulong? GetGlobalStakingSharesValue(this ICollection<TealKeyValue> values) {
			return values
				.FirstOrDefault(s => String.Equals(s.Key, GlobalStakingSharesKey, mCmp))?
				.Value?
				.Uint;
		}

		public static ulong? GetTotalClaimableYieldlyValue(this ICollection<TealKeyValue> values) {
			return values
				.FirstOrDefault(s => String.Equals(s.Key, TotalClaimableYieldlyKey, mCmp))?
				.Value?
				.Uint;
		}

		public static ulong? GetTotalClaimableAlgoValue(this ICollection<TealKeyValue> values) {
			return values
				.FirstOrDefault(s => String.Equals(s.Key, TotalClaimableAlgoKey, mCmp))?
				.Value?
				.Uint;
		}

		public static ulong? GetTotalClaimableAsaValue(this ICollection<TealKeyValue> values) {
			return values
				.FirstOrDefault(s => String.Equals(s.Key, TotalClaimableAsaKey, mCmp))?
				.Value?
				.Uint;
		}

		public static ulong? GetUserStakingShareValue(this ICollection<TealKeyValue> values) {
			return values
				.FirstOrDefault(s => String.Equals(s.Key, UserStakingSharesKey, mCmp))?
				.Value?
				.Uint;
		}

		public static ulong? GetUserAmountValue(this ICollection<TealKeyValue> values) {
			return values
				.FirstOrDefault(s => String.Equals(s.Key, UserAmountKey, mCmp))?
				.Value?
				.Uint;
		}

		public static ulong? GetUserTimeValue(this ICollection<TealKeyValue> values) {
			return values
				.FirstOrDefault(s => String.Equals(s.Key, UserTimeKey, mCmp))?
				.Value?
				.Uint;
		}

	}

}

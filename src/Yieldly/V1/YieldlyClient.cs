using Algorand;
using Algorand.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Yieldly.V1.Model;

namespace Yieldly.V1 {

	public class YieldlyClient {

		private readonly AlgodApi mAlgodApi;
		private readonly ulong mStakingAppId;

		public YieldlyClient(
			AlgodApi algodApi, ulong stakingAppId) {

			mAlgodApi = algodApi;
			mStakingAppId = stakingAppId;
		}

		public virtual ulong FetchYieldlyAmountStaked(Address address) {

			var stakingAppId = (long)mStakingAppId;
			var accountInfo = mAlgodApi.AccountInformation(address.EncodeAsString());

			var stakingAppLocalState = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == stakingAppId)?
				.KeyValue;

			if (stakingAppLocalState == null) {
				return 0;
			}

			var ua = stakingAppLocalState.GetUserAmountValue();

			return ua.GetValueOrDefault();
		}

		public virtual ClaimableAmountResult FetchClaimableAmount(Address address) {

			var stakingAppId = (long)mStakingAppId;
			var accountInfo = mAlgodApi.AccountInformation(address.EncodeAsString());
			var application = mAlgodApi.GetApplicationByID(stakingAppId);

			var stakingAppLocalState = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == stakingAppId)?
				.KeyValue;

			var stakingAppGlobalState = application?
				.Params?
				.GlobalState;

			if (stakingAppLocalState == null || stakingAppGlobalState == null) {
				return null;
			}

			var gt = stakingAppGlobalState.GetGlobalTimeValue();
			var gss = stakingAppGlobalState.GetGlobalStakingSharesValue();
			var tyul = stakingAppGlobalState.GetTotalClaimableYieldlyValue();
			var tap = stakingAppGlobalState.GetTotalClaimableAlgoValue();

			var uss = stakingAppLocalState.GetUserStakingShareValue();
			var ua = stakingAppLocalState.GetUserAmountValue();
			var ut = stakingAppLocalState.GetUserTimeValue();

			var time = BigInteger.Multiply((gt.Value - ut.Value) / 86400, ua.Value);
			var share = (double)(uss.Value + time) / (double)gss.Value;
			var claimableAlgo = share * tap.Value;
			var claimableYieldly = share * tyul.Value;

			return new ClaimableAmountResult {
				Algo = (ulong)claimableAlgo,
				Yieldly = (ulong)claimableYieldly
			};
		}






	}
}

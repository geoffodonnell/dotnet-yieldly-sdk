using Algorand.V2.Model;
using System.Linq;
using System.Numerics;
using Yieldly.V1.Model;

namespace Yieldly.V1 {

	public static class YieldlyEquation {

		public static RewardAmounts CalculateClaimableAmount(
			Account accountInfo, Application application) {

			var stakingAppLocalState = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == application.Id)?
				.KeyValue;

			var stakingAppGlobalState = application?
				.Params?
				.GlobalState;

			if (stakingAppLocalState == null || stakingAppGlobalState == null) {
				return null;
			}

			var gt = stakingAppGlobalState.GetGlobalTimeValue().GetValueOrDefault();
			var gss = stakingAppGlobalState.GetGlobalStakingSharesValue().GetValueOrDefault();
			var tyul = stakingAppGlobalState.GetTotalClaimableYieldlyValue().GetValueOrDefault();
			var tap = stakingAppGlobalState.GetTotalClaimableAlgoValue().GetValueOrDefault();

			var uss = stakingAppLocalState.GetUserStakingShareValue().GetValueOrDefault();
			var ua = stakingAppLocalState.GetUserAmountValue().GetValueOrDefault();
			var ut = stakingAppLocalState.GetUserTimeValue().GetValueOrDefault();

			var time = BigInteger.Multiply((ulong)((double)(gt - ut) / 86400), ua);
			var share = (double)(uss + time) / (double)gss;
			var claimableAlgo = share * tap;
			var claimableYieldly = share * tyul;

			return new RewardAmounts {
				Algo = (ulong)claimableAlgo,
				Yieldly = (ulong)claimableYieldly
			};
		}

	}

}

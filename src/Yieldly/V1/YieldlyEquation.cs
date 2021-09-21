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

			var gt = stakingAppGlobalState.GetGlobalTimeValue();
			var gss = stakingAppGlobalState.GetGlobalStakingSharesValue();
			var tyul = stakingAppGlobalState.GetTotalClaimableYieldlyValue();
			var tap = stakingAppGlobalState.GetTotalClaimableAlgoValue();

			var uss = stakingAppLocalState.GetUserStakingShareValue();
			var ua = stakingAppLocalState.GetUserAmountValue();
			var ut = stakingAppLocalState.GetUserTimeValue();

			var time = BigInteger.Multiply((gt.Value - ut.Value) / 86400, ua.Value);
			var share = (double)(uss.Value + time) / (double)gss.Value;
			var claimableAlgo = share * tap.GetValueOrDefault();
			var claimableYieldly = share * tyul.GetValueOrDefault();

			return new RewardAmounts {
				Algo = (ulong)claimableAlgo,
				Yieldly = (ulong)claimableYieldly
			};
		}

	}

}

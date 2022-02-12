using Algorand.V2.Algod.Model;
using System.Linq;
using System.Numerics;
using Yieldly.V1.Model;

namespace Yieldly.V1 {

	public static class YieldlyEquation {

		public static LotteryRewardAmount CalculateLotteryClaimableAmount(
			Account accountInfo, Application application) {

			var share = GetUserShareOfClaimableTotalAmount(accountInfo, application);

			if (!share.HasValue) {
				return null;
			}

			var stakingAppGlobalState = application?
				.Params?
				.GlobalState;

			var tyul = stakingAppGlobalState.GetTotalClaimableYieldlyValue().GetValueOrDefault();
			var claimableYieldly = share * tyul;

			return new LotteryRewardAmount {
				Yieldly = (ulong)claimableYieldly
			};
		}

		public static StakingRewardAmount CalculateStakingClaimableAmount(
			Account accountInfo, Application application) {

			var share = GetUserShareOfClaimableTotalAmount(accountInfo, application);

			if (!share.HasValue) {
				return null;
			}

			var stakingAppGlobalState = application?
			.Params?
			.GlobalState;

			var tyul = stakingAppGlobalState.GetTotalClaimableYieldlyValue().GetValueOrDefault();
			var tap = stakingAppGlobalState.GetTotalClaimableAlgoValue().GetValueOrDefault();			
			var claimableAlgo = share * tap;
			var claimableYieldly = share * tyul;

			return new StakingRewardAmount {
				Algo = (ulong)claimableAlgo,
				Yieldly = (ulong)claimableYieldly
			};
		}

		public static AsaStakingRewardAmount CalculateAsaStakePoolClaimableAmount(
			Account accountInfo, Application application) {

			var share = GetUserShareOfClaimableTotalAmount(accountInfo, application);

			if (!share.HasValue) {
				return null;
			}

			var stakingAppGlobalState = application?
				.Params?
				.GlobalState;

			var tyul = stakingAppGlobalState.GetTotalClaimableYieldlyValue().GetValueOrDefault();
			var claimableAmount = share * tyul;

			return new AsaStakingRewardAmount {
				Asa = (ulong)claimableAmount
			};
		}

		public static AsaStakingRewardAmount CalculateAsaStakePoolClaimableAmountTeal5(
			Account accountInfo, Application application, ulong time) {

			/* User values */
			var userDebt = YieldlyUtils.GetBigInteger(accountInfo.AppsLocalState, application.Id, "User_Debt_1");
			var userStake = YieldlyUtils.GetBigInteger(accountInfo.AppsLocalState, application.Id, "User_Stake");
			var userClaimable = YieldlyUtils.GetBigInteger(accountInfo.AppsLocalState, application.Id, "User_Claimable_1");

			/* Application values */
			var startDate = (ulong)YieldlyUtils.GetBigInteger(application.Params.GlobalState, "Start_Date");
			var endDate = (ulong)YieldlyUtils.GetBigInteger(application.Params.GlobalState, "End_Date");
			var rewardsLocked = (ulong)YieldlyUtils.GetBigInteger(application.Params.GlobalState, "Rewards_Locked_1");
			var rewardsUnlocked = (ulong)YieldlyUtils.GetBigInteger(application.Params.GlobalState, "Rewards_Unlocked_1");
			var precision = YieldlyUtils.GetBigInteger(application.Params.GlobalState, "Precision");
			var globalStake = YieldlyUtils.GetBigInteger(application.Params.GlobalState, "Global_Stake");

			/* Maximum value for 'time' is the value of End_Date */
			if (endDate < time) {
				time = endDate;
			}

			var totalUnlocked = (ulong)((double)(time - startDate) / (double)(endDate - startDate) * (double)rewardsLocked);
			var remainingUnlocked = totalUnlocked - rewardsUnlocked;

			var tokenPerShare = YieldlyUtils.GetBigInteger(application.Params.GlobalState, "Token_Per_Share_1");
			var remainingUnlockedShare = BigInteger.Divide(BigInteger.Multiply(remainingUnlocked, precision), globalStake) + tokenPerShare;
			var remainingUnlockedUserShare = BigInteger.Divide(BigInteger.Multiply(remainingUnlockedShare, userStake), precision);

			BigInteger result;

			if (remainingUnlockedUserShare < userDebt) {
				result = userClaimable + remainingUnlockedUserShare;
			} else {
				result = userClaimable + remainingUnlockedUserShare - userDebt;
			}

			return new AsaStakingRewardAmount {
				Asa = (ulong)result
			};
		}

		private static double? GetUserShareOfClaimableTotalAmount(
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

			var uss = stakingAppLocalState.GetUserStakingShareValue().GetValueOrDefault();
			var ua = stakingAppLocalState.GetUserAmountValue().GetValueOrDefault();
			var ut = stakingAppLocalState.GetUserTimeValue().GetValueOrDefault();

			var time = BigInteger.Multiply((ulong)((double)(gt - ut) / 86400), ua);
			var result = (double)(uss + time) / (double)gss;

			return result;
		}

	}

}

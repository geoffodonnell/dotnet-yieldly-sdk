namespace Yieldly.V1.Model {

	public class FetchAmountsResult {

		public RewardAmounts NoLossLotteryReward { get; set; }

		public RewardAmounts StakingReward { get; set; }

		public ulong AlgoInNoLossLottery { get; set; }

		public ulong YieldlyStaked { get; set; }

	}

}

namespace Yieldly.V1.Model {

	public class FetchAmountsResult {

		public RewardAmounts LotteryReward { get; set; }

		public RewardAmounts StakingReward { get; set; }

		public ulong AlgoInLottery { get; set; }

		public ulong YieldlyStaked { get; set; }

	}

}

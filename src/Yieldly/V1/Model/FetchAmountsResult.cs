namespace Yieldly.V1.Model {

	public class FetchAmountsResult {

		public LotteryRewardAmount LotteryReward { get; set; }

		public StakingRewardAmount StakingReward { get; set; }

		public ulong AlgoInLottery { get; set; }

		public ulong YieldlyStaked { get; set; }

	}

}

using Algorand;
using Algorand.V2;
using Algorand.V2.Model;
using System;
using System.Linq;
using Yieldly.V1.Model;

namespace Yieldly.V1 {

	public class YieldlyClient {

		private readonly AlgodApi mAlgodApi;

		internal AlgodApi AlgodApi { get => mAlgodApi; }

		/// <summary>
		/// Construct a new instance which connects to the default node
		/// </summary>
		public YieldlyClient() :
			this(new AlgodApi(Constant.AlgodMainnetHost, String.Empty)) { }

		/// <summary>
		/// Construct a new instance
		/// </summary>
		/// <param name="algodApi">Algod API connection</param>
		public YieldlyClient(
			AlgodApi algodApi) {

			mAlgodApi = algodApi;
		}

		/// <summary>
		/// Submit a signed transaction group.
		/// </summary>
		/// <param name="transactionGroup">Signed transaction group</param>
		/// <param name="wait">Wait for confirmation</param>
		/// <returns>Transaction reponse</returns>
		public virtual PostTransactionsResponse Submit(
			TransactionGroup transactionGroup, bool wait = true) {

			return transactionGroup.Submit(mAlgodApi, wait);
		}

		public virtual FetchAmountsResult FetchAmounts(Address address) {

			var accountInfo = mAlgodApi.AccountInformation(address.EncodeAsString());
			var lotteryApp = mAlgodApi.GetApplicationByID((long)Constant.LotteryAppId);
			var stakingApp = mAlgodApi.GetApplicationByID((long)Constant.StakingAppId);

			var lotteryReward = YieldlyEquation.CalculateLotteryClaimableAmount(accountInfo, lotteryApp);
			var stakingReward = YieldlyEquation.CalculateStakingClaimableAmount(accountInfo, stakingApp);
			var algoInLottery = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == lotteryApp.Id)?
				.KeyValue?
				.GetUserAmountValue();
			var yieldlyStaked = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == stakingApp.Id)?
				.KeyValue?
				.GetUserAmountValue()
				.GetValueOrDefault();

			return new FetchAmountsResult { 
				LotteryReward = lotteryReward,
				StakingReward = stakingReward,
				AlgoInLottery = algoInLottery.GetValueOrDefault(),
				YieldlyStaked = yieldlyStaked.GetValueOrDefault()
			};
		}

	}

}

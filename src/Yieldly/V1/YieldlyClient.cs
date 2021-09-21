using Algorand;
using Algorand.V2;
using Algorand.V2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Yieldly.V1.Model;
using Account = Algorand.V2.Model.Account;

namespace Yieldly.V1 {

	public class YieldlyClient {

		private readonly AlgodApi mAlgodApi;

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

		public virtual FetchAmountsResult FetchYieldlyAmounts(Address address) {

			var accountInfo = mAlgodApi.AccountInformation(address.EncodeAsString());
			var noLossLotteryApp = mAlgodApi.GetApplicationByID((long)Constant.NoLossLotteryAppId);
			var stakingApp = mAlgodApi.GetApplicationByID((long)Constant.StakingAppId);

			var noLossLotteryReward = YieldlyEquation.CalculateClaimableAmount(accountInfo, noLossLotteryApp);
			var stakingReward = YieldlyEquation.CalculateClaimableAmount(accountInfo, stakingApp);
			var algoInNoLossLottery = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == noLossLotteryApp.Id)?
				.KeyValue?
				.GetUserAmountValue();
			var yieldlyStaked = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == stakingApp.Id)?
				.KeyValue?
				.GetUserAmountValue()
				.GetValueOrDefault();

			return new FetchAmountsResult { 
				NoLossLotteryReward = noLossLotteryReward,
				StakingReward = stakingReward,
				AlgoInNoLossLottery = algoInNoLossLottery.GetValueOrDefault(),
				YieldlyStaked = yieldlyStaked.GetValueOrDefault()
			};
		}

	}

}

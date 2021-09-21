using Algorand;
using Algorand.V2.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Yieldly.V1.Model;
using Account = Algorand.Account;

namespace Yieldly.V1 {

	public static class YieldlyClientExtensions {

		public static PostTransactionsResponse NoLossLotteryDeposit(
			this YieldlyClient client,
			Account account,
			ulong algoAmount) {

			var txs = PrepareNoLossLotteryDepositTransactions(
				client, account.Address, algoAmount);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static TransactionGroup PrepareNoLossLotteryDepositTransactions(
			this YieldlyClient client,
			Address sender,
			ulong algoAmount) {

			var txParams = client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareNoLossLotteryDepositTransactions(algoAmount, sender, txParams);

			return result;
		}

	}

}

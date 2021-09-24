using Algorand;
using Algorand.V2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yieldly.V1.Model;
using Account = Algorand.Account;

namespace Yieldly.V1 {

	public static class YieldlyClientExtensions {

		public static PostTransactionsResponse OptIn(
			this YieldlyClient client,
			Account account,
			bool includeYieldlyAsa = true,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true) {

			var txs = PrepareOptInTransactions(
				client,
				account.Address,
				includeYieldlyAsa,
				includeProxyContract,
				includeStakingContract,
				includeLotteryContract);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static PostTransactionsResponse OptOut(
			this YieldlyClient client,
			Account account,
			bool includeYieldlyAsa = true,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true,
			bool checkAssetBalance = true) {

			var yieldlyAssetId = (long)Constant.YieldlyAssetId;

			if (checkAssetBalance) {
				var accountInfo = client.AlgodApi
					.AccountInformation(account.Address.EncodeAsString());

				var assetInfo = accountInfo.Assets
					.FirstOrDefault(s => s.AssetId.GetValueOrDefault() == yieldlyAssetId);

				if (assetInfo?.Amount.GetValueOrDefault() > 0) {
					throw new Exception("Attempting Yieldly ASA opt-out with non-zero balance.");
				}
			}

			var txs = PrepareOptOutTransactions(
				client,
				account.Address,
				includeYieldlyAsa,
				includeProxyContract,
				includeStakingContract,
				includeLotteryContract);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static PostTransactionsResponse LotteryDeposit(
			this YieldlyClient client,
			Account account,
			ulong algoAmount) {

			var txs = PrepareLotteryDepositTransactions(
				client, account.Address, algoAmount);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static PostTransactionsResponse LotteryWithdraw(
			this YieldlyClient client,
			Account account,
			ulong algoAmount) {

			var txs = PrepareLotteryWithdrawTransactions(
				client, account.Address, algoAmount);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static PostTransactionsResponse LotteryClaimReward(
			this YieldlyClient client,
			Account account,
			ulong yieldlyAmount) {

			var txs = PrepareLotteryClaimRewardTransactions(
				client, account.Address, yieldlyAmount);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static PostTransactionsResponse LotteryClaimWinning(
			this YieldlyClient client,
			Account account,
			ulong algoAmount) {

			var txs = PrepareLotteryClaimWinningTransactions(
				client, account.Address, algoAmount);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static PostTransactionsResponse YieldlyStakingDeposit(
			this YieldlyClient client,
			Account account,
			ulong yieldlyAmount) {

			var txs = PrepareYieldlyStakingDepositTransactions(
				client, account.Address, yieldlyAmount);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static PostTransactionsResponse YieldlyStakingWithdraw(
			this YieldlyClient client,
			Account account,
			ulong yieldlyAmount) {

			var txs = PrepareYieldlyStakingWithdrawTransactions(
				client, account.Address, yieldlyAmount);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static PostTransactionsResponse YieldyStakingClaimReward(
			this YieldlyClient client,
			Account account,
			RewardAmounts amounts) {

			var txs = PrepareYieldlyStakingClaimRewardTransactions(
				client, account.Address, amounts);

			txs.Sign(account);

			return client.Submit(txs, true);
		}

		public static TransactionGroup PrepareOptInTransactions(
			this YieldlyClient client,
			Address sender, 
			bool includeYieldlyAsa = true,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true) {

			var txParams = client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareOptInTransactions(
					sender,
					txParams,
					includeYieldlyAsa,
					includeProxyContract,
					includeStakingContract,
					includeLotteryContract);

			return result;
		}

		public static TransactionGroup PrepareOptOutTransactions(
			this YieldlyClient client,
			Address sender,
			bool includeYieldlyAsa = true,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true) {

			var txParams = client.AlgodApi.TransactionParams();
			var asset = client.AlgodApi.GetAssetByID((long)Constant.YieldlyAssetId);

			var closeTo = new Address(asset.Params.Creator);

			var result = YieldlyTransaction
				.PrepareOptOutTransactions(
					sender,
					closeTo,
					txParams,
					includeYieldlyAsa,
					includeProxyContract,
					includeStakingContract,
					includeLotteryContract);

			return result;
		}

		public static TransactionGroup PrepareLotteryDepositTransactions(
			this YieldlyClient client,
			Address sender,
			ulong algoAmount) {

			var txParams = client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareLotteryDepositTransactions(algoAmount, sender, txParams);

			return result;
		}

		public static TransactionGroup PrepareLotteryWithdrawTransactions(
			this YieldlyClient client,
			Address sender,
			ulong algoAmount) {

			var txParams = client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareLotteryWithdrawTransactions(algoAmount, sender, txParams);

			return result;
		}

		public static TransactionGroup PrepareLotteryClaimRewardTransactions(
			this YieldlyClient client,
			Address sender,
			ulong yieldlyAmount) {

			var txParams = client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareLotteryClaimRewardTransactions(
					yieldlyAmount, sender, txParams);

			return result;
		}

		public static TransactionGroup PrepareLotteryClaimWinningTransactions(
			this YieldlyClient client,
			Address sender,
			ulong algoAmount) {

			var txParams = client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareLotteryClaimWinningTransactions(
					algoAmount, sender, txParams);

			return result;
		}

		public static TransactionGroup PrepareYieldlyStakingDepositTransactions(
			this YieldlyClient client,
			Address sender,
			ulong yieldlyAmount) {

			var txParams = client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareYieldlyStakingDepositTransactions(yieldlyAmount, sender, txParams);

			return result;
		}

		public static TransactionGroup PrepareYieldlyStakingWithdrawTransactions(
			this YieldlyClient client,
			Address sender,
			ulong yieldlyAmount) {

			var txParams = client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareYieldlyStakingWithdrawTransactions(yieldlyAmount, sender, txParams);

			return result;
		}

		public static TransactionGroup PrepareYieldlyStakingClaimRewardTransactions(
			this YieldlyClient client,
			Address sender,
			RewardAmounts amounts) {

			var txParams = client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareYieldlyStakingClaimRewardTransactions(
					amounts.Algo, amounts.Yieldly, sender, txParams);

			return result;
		}

	}

}

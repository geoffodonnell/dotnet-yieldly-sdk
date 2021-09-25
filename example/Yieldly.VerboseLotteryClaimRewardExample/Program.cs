using Algorand;
using System;
using System.Configuration;
using Yieldly.V1;

namespace Yieldly.VerboseLotteryClaimRewardExample {

	public class Program {

		static void Main(string[] args) {

			var settings = ConfigurationManager.AppSettings;
			var mnemonic = settings.Get("Account.Mnemonic");

			if (String.IsNullOrWhiteSpace(mnemonic)) {
				throw new Exception("'Account.Mnemonic' key is not set in App.Config.");
			}

			var account = new Account(mnemonic);

			// Initialize the client
			var algodApi = new Algorand.V2.AlgodApi(
				Constant.AlgodMainnetHost, String.Empty);
			var client = new YieldlyClient(algodApi);

			// Fetch all Yieldly amounts
			var amounts = client.FetchAmounts(account.Address);

			// Claim current Yieldy rewards from lottery
			try {

				var txParams = algodApi.TransactionParams();

				var lotteryClaimTxGroup = YieldlyTransaction
					.PrepareLotteryClaimRewardTransactions(
						amounts.LotteryReward.Yieldly, account.Address, txParams);

				// Sign the transactions sent from the account,
				// the LogicSig transactions will already be signed
				for (var i = 0; i < lotteryClaimTxGroup.Transactions.Length; i++) {
					var tx = lotteryClaimTxGroup.Transactions[i];

					if (tx.sender.Equals(account.Address)) {

						// Inspect transaction

						// Sign transaction
						lotteryClaimTxGroup.SignedTransactions[i] = account.SignTransaction(tx);
					}
				}

				var lotteryClaimResult = client.Submit(lotteryClaimTxGroup);

				Console.WriteLine($"Lottery reward claim complete, transaction ID: {lotteryClaimResult.TxId}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

using Algorand;
using System;
using System.Configuration;
using Yieldly.V1;

namespace Yieldly.VerboseYieldlyStakingWithdrawExample {

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

			// Withdraw all YLDY currently deposited in the Yieldly staking pool
			try {
				var txParams = algodApi.TransactionParams();

				var stakingWithdrawTxGroup = YieldlyTransaction
					.PrepareYieldlyStakingWithdrawTransactions(
						amounts.YieldlyStaked, account.Address, txParams);

				// Sign the transactions sent from the account,
				// the LogicSig transactions will already be signed
				for (var i = 0; i < stakingWithdrawTxGroup.Transactions.Length; i++) {
					var tx = stakingWithdrawTxGroup.Transactions[i];

					if (tx.sender.Equals(account.Address)) {

						// Inspect transaction

						// Sign transaction
						stakingWithdrawTxGroup.SignedTransactions[i] = account.SignTransaction(tx);
					}
				}

				var stakingWithdrawResult = client.Submit(stakingWithdrawTxGroup);

				Console.WriteLine($"Yieldly staking withdraw complete, transaction ID: {stakingWithdrawResult.TxId}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

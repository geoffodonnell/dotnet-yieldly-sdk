using Algorand;
using System;
using System.Configuration;
using Yieldly.V1;

namespace Yieldly.VerboseYieldlyStakingDepositExample {

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

			var amountToDeposit = YieldlyUtils.YieldlyToMicroyieldly(1000.0);

			// It might be a good idea to ensure the account balance allows for a 1000 YLDY transfer 

			// Deposit 1000 YLDY in the Yieldly staking pool
			try {
				//var result = client.YieldlyStakingDeposit(account, amountToDeposit);

				var txParams = algodApi.TransactionParams();

				var stakingDepositTxGroup = YieldlyTransaction
					.PrepareYieldlyStakingDepositTransactions(
						amountToDeposit, account.Address, txParams);

				// Sign the transactions sent from the account,
				// the LogicSig transactions will already be signed
				for (var i = 0; i < stakingDepositTxGroup.Transactions.Length; i++) {
					var tx = stakingDepositTxGroup.Transactions[i];

					if (tx.sender.Equals(account.Address)) {

						// Inspect transaction

						// Sign transaction
						stakingDepositTxGroup.SignedTransactions[i] = account.SignTransaction(tx);
					}
				}

				var stakingDepositResult = client.Submit(stakingDepositTxGroup);

				Console.WriteLine($"Yieldly staking deposit complete, transaction ID: {stakingDepositResult.TxId}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

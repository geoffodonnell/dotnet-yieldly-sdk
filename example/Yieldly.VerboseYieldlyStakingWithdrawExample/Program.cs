using Algorand;
using Algorand.V2;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Yieldly.V1;

namespace Yieldly.VerboseYieldlyStakingWithdrawExample {

	public class Program {

		public static async Task Main(string[] args) {

			var settings = ConfigurationManager.AppSettings;
			var mnemonic = settings.Get("Account.Mnemonic");

			if (String.IsNullOrWhiteSpace(mnemonic)) {
				throw new Exception("'Account.Mnemonic' key is not set in App.Config.");
			}

			var account = new Account(mnemonic);

			// Initialize the client
			var url = Constant.AlgodMainnetHost;
			var token = String.Empty;
			var httpClient = HttpClientConfigurator.ConfigureHttpClient(url, token);
			var client = new YieldlyClient(httpClient, url);

			// Fetch all Yieldly amounts
			var amounts = await client.FetchAmountsAsync(account.Address);

			// Withdraw all YLDY currently deposited in the Yieldly staking pool
			try {
				var txParams = await client.FetchTransactionParamsAsync();

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

				var stakingWithdrawResult = await client.SubmitAsync(stakingWithdrawTxGroup);

				Console.WriteLine($"Yieldly staking withdraw complete, transaction ID: {stakingWithdrawResult.TxId}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

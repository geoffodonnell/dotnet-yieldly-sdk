using Algorand;
using Algorand.Algod.Model;
using Algorand.Utils;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Yieldly.V1;

namespace Yieldly.VerboseLotteryDepositExample {

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

			var amountToDeposit = Utils.AlgosToMicroalgos(10.0);

			// It might be a good idea to ensure the account balance allows for a 10 ALGO transfer 

			// Deposit 10 ALGO in the no loss lottery
			try {
				var txParams = await client.FetchTransactionParamsAsync();

				var lotteryDepositTxGroup = YieldlyTransaction
					.PrepareLotteryDepositTransactions(
						amountToDeposit, account.Address, txParams);

				// Sign the transactions sent from the account,
				// the LogicSig transactions will already be signed
				for (var i = 0; i < lotteryDepositTxGroup.Transactions.Length; i++) {
					var tx = lotteryDepositTxGroup.Transactions[i];

					if (tx.Sender.Equals(account.Address)) {

						// Inspect transaction

						// Sign transaction
						lotteryDepositTxGroup.SignedTransactions[i] = tx.Sign(account);
					}
				}

				var lotteryDepositResult = await client.SubmitAsync(lotteryDepositTxGroup);

				Console.WriteLine($"Lottery deposit complete, transaction ID: {lotteryDepositResult.Txid}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

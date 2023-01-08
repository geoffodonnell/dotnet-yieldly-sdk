using Algorand;
using Algorand.Algod.Model;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Yieldly.V1;

namespace Yieldly.VerboseYieldlyStakingClaimRewardExample {

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

			// Withdraw all ALGO and YLDY currently available as rewards from Yieldly staking pool participation
			try {

				var txParams = await client.FetchTransactionParamsAsync();

				var stakingClaimTxGroup = YieldlyTransaction
					.PrepareYieldlyStakingClaimRewardTransactions(
						amounts.StakingReward.Algo, amounts.StakingReward.Yieldly, account.Address, txParams);

				// Sign the transactions sent from the account,
				// the LogicSig transactions will already be signed
				for (var i = 0; i < stakingClaimTxGroup.Transactions.Length; i++) {
					var tx = stakingClaimTxGroup.Transactions[i];

					if (tx.Sender.Equals(account.Address)) {

						// Inspect transaction

						// Sign transaction
						stakingClaimTxGroup.SignedTransactions[i] = tx.Sign(account);
					}
				}

				var stakingClaimResult = await client.SubmitAsync(stakingClaimTxGroup);

				Console.WriteLine($"Yieldly staking deposit complete, transaction ID: {stakingClaimResult.Txid}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

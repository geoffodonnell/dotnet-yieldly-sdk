using Algorand;
using Algorand.V2;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Yieldly.V1;

namespace Yieldly.YieldlyStakingDepositExample {

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

			var amountToDeposit = YieldlyUtils.YieldlyToMicroyieldly(1000.0);

			// It might be a good idea to ensure the account balance allows for a 1000 YLDY transfer 

			// Deposit 1000 YLDY in the Yieldly staking pool
			try {
				var result = await client.YieldlyStakingDepositAsync(account, amountToDeposit);

				Console.WriteLine($"Yieldly staking deposit complete, transaction ID: {result.TxId}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

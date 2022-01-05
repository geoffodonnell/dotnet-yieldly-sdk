using Algorand;
using Algorand.V2;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Yieldly.V1;

namespace Yieldly.LotteryDepositExample {
	
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
				var result = await client.LotteryDepositAsync(account, amountToDeposit);

				Console.WriteLine($"Lottery deposit complete, transaction ID: {result.TxId}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

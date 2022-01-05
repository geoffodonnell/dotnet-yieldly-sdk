using Algorand;
using Algorand.V2;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Yieldly.V1;

namespace Yieldly.LotteryWithdrawExample {
	
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

			// Withdraw all ALGO currently deposited in the no loss lottery
			try {
				var result = await client.LotteryWithdrawAsync(account, amounts.AlgoInLottery);

				Console.WriteLine($"Lottery widthdrawal complete, transaction ID: {result.TxId}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

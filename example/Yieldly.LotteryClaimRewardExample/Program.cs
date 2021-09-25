using Algorand;
using System;
using System.Configuration;
using Yieldly.V1;

namespace Yieldly.LotteryClaimRewardExample {

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
				var result = client.LotteryClaimReward(account, amounts.LotteryReward.Yieldly);

				Console.WriteLine($"Lottery reward claim complete, transaction ID: {result.TxId}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

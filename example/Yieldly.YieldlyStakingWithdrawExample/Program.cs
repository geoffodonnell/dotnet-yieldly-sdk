﻿using Algorand;
using Algorand.Algod.Model;
using System;
using System.Configuration;
using System.Threading.Tasks;
using Yieldly.V1;

namespace Yieldly.YieldlyStakingWithdrawExample {

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
				var result = await client.YieldlyStakingWithdrawAsync(account, amounts.YieldlyStaked);

				Console.WriteLine($"Yieldly staking withdraw complete, transaction ID: {result.Txid}");

			} catch (Exception ex) {
				Console.WriteLine($"An error occured: {ex.Message}");
			}

			Console.WriteLine("Example complete.");
		}

	}

}

using Algorand;
using Algorand.V2.Algod.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yieldly.V1.Model;
using Account = Algorand.Account;
using AccountInformation = Algorand.V2.Algod.Model.Account;

namespace Yieldly.V1 {

	public class AsaStakingPool {

		public ulong ApplicationId { get; set; }

		public SimpleAsset StakeAsset { get; set; }

		public SimpleAsset RewardAsset { get; set; }

		public string Address { get; set; }

		internal YieldlyClient Client { get; set; }

		internal LogicsigSignature LogicsigSignature { get; set; }

		internal Application Application { get; set; }

		/// <summary>
		/// Refresh the pool information.
		/// </summary>
		public async Task RefreshAsync() {

			Application = await Client.DefaultApi.ApplicationsAsync((int)ApplicationId);
		}

		/// <summary>
		/// Check whether or not the address is opted-in to the pool application.
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <returns>Whether or not the address is opted-in</returns>
		public virtual async Task<bool> IsOptedInAsync(Address address) {

			var info = await Client.DefaultApi
				.AccountsAsync(address.EncodeAsString(), Format.Json);
			var appId = ApplicationId;

			foreach (var entry in info.AppsLocalState) {

				if (entry.Id == appId) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Check whether or not the address is opted-in to the staking asset.
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <returns>Whether or not the address is opted-in</returns>
		public virtual async Task<bool> IsOptedInToStakeAssetAsync(Address address) {

			var info = await Client.DefaultApi
				.AccountsAsync(address.EncodeAsString(), Format.Json);

			return AccountIsOptedInToAsset(info, StakeAsset.Id);
		}

		/// <summary>
		/// Check whether or not the address is opted-in to the reward asset.
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <returns>Whether or not the address is opted-in</returns>
		public virtual async Task<bool> IsOptedInToRewardAssetAsync(Address address) {
			
			var info = await Client.DefaultApi
				.AccountsAsync(address.EncodeAsString(), Format.Json);

			return AccountIsOptedInToAsset(info, RewardAsset.Id);
		}

		protected virtual bool AccountIsOptedInToAsset(
			AccountInformation accountInfo, ulong assetId) {

			return accountInfo
				.Assets
				.Any(s => s.AssetId == assetId);
		}

		/// <summary>
		/// Fetch amounts from pool
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <param name="refreshPoolInfo">Whether or not the pool information should be refreshed</param>
		/// <returns>Pool amounts</returns>
		public virtual async Task<FetchAsaStakingAmountsResult> FetchAmountsAsync(
			Address address, bool refreshPoolInfo = false) {

			var info = await Client.DefaultApi
				.AccountsAsync(address.EncodeAsString(), Format.Json);

			if (refreshPoolInfo) {
				await RefreshAsync();
			}

			var reward = YieldlyEquation.CalculateAsaStakePoolClaimableAmount(info, Application);
			var staked = info?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == ApplicationId)?
				.KeyValue?
				.GetUserAmountValue()
				.GetValueOrDefault();

			return new FetchAsaStakingAmountsResult {
				Reward = reward?.Asa ?? 0ul,
				Staked = staked.GetValueOrDefault()
			};
		}

		/// <summary>
		/// Opt-in to the pool, optionally opt-in to staking and reward assets (if neccessary).
		/// 
		/// NOTE: Unlike other operations, these transactions cannot be issued in a single
		/// transaction group. Therefore, opting-in to asset(s) here will result in submitting
		/// multiple transactions.
		/// </summary>
		/// <param name="account">Account to opt-in</param>
		/// <param name="optInToStakeAsset">Whether or not to opt-in to the stake asset</param>
		/// <param name="optInToRewardAsset">Whether or not to opt-in to the reward asset</param>
		/// <returns>Transaction responses</returns>
		public virtual async Task<PostTransactionsResponse[]> OptInAsync(
			Account account,
			bool optInToStakeAsset = true,
			bool optInToRewardAsset = true,
			bool wait = true) {

			var results = new List<PostTransactionsResponse>();

			AccountInformation accountInfo = (optInToStakeAsset || optInToRewardAsset)
				? await Client.DefaultApi.AccountsAsync(account.Address.EncodeAsString(), Format.Json)
				: null;

			// If neccessary, opt-in to stake asset
			if (optInToStakeAsset && !AccountIsOptedInToAsset(accountInfo, StakeAsset.Id)) {
				var stakeAssetOptInTxs = await PrepareStakeAssetOptInTransactionsAsync(account.Address);
				
				stakeAssetOptInTxs.Sign(account);

				var stakeAssetOptInResult = await Client.SubmitAsync(stakeAssetOptInTxs, true);

				results.Add(stakeAssetOptInResult);
			}

			// If neccessary, opt-in to reward asset
			if (optInToRewardAsset && !AccountIsOptedInToAsset(accountInfo, RewardAsset.Id)) {
				var rewardAssetOptInTxs = await PrepareRewardAssetOptInTransactionsAsync(account.Address);

				rewardAssetOptInTxs.Sign(account);

				var rewardAssetOptInResult = await Client.SubmitAsync(rewardAssetOptInTxs, true);

				results.Add(rewardAssetOptInResult);
			}

			// Finally, opt-in to app
			var appOptInTxs = await PrepareOptInTransactionsAsync(account.Address);

			appOptInTxs.Sign(account);

			var appOptInResult = await Client.SubmitAsync(appOptInTxs, wait);

			results.Add(appOptInResult);

			return results.ToArray();
		}

		/// <summary>
		/// Opt-out of the stake pool application and, optionally, the reward asset.
		/// </summary>
		/// <param name="account">Account to opt-out</param>
		/// <param name="optOutOfRewardAsset">Whether or not to opt-out of the reward asset</param>
		/// <param name="checkRewardAssetBalance">Whether or not to check the balance of reward asset before opting-out</param>
		/// <returns>Transaction response</returns>
		public virtual async Task<PostTransactionsResponse> OptOutAsync(
			Account account,
			bool optOutOfRewardAsset = false,
			bool checkRewardAssetBalance = true,
			bool wait = true) {

			var rewardAssetId = RewardAsset.Id;

			if (checkRewardAssetBalance) {
				var accountInfo = await Client.DefaultApi
					.AccountsAsync(account.Address.EncodeAsString(), Format.Json);

				var assetInfo = accountInfo.Assets
					.FirstOrDefault(s => s.AssetId == rewardAssetId);

				if (assetInfo.Amount > 0) {
					throw new Exception($"Attempting {RewardAsset.Name} ASA opt-out with non-zero balance.");
				}
			}

			var txs = await PrepareOptOutTransactionsAsync(
				account.Address, optOutOfRewardAsset);

			txs.Sign(account);

			return await Client.SubmitAsync(txs, wait);
		}

		/// <summary>
		/// Deposit into the stake pool.
		/// </summary>
		/// <param name="account">Account to make deposit</param>
		/// <param name="stakeAmount">Amount to deposit</param>
		/// <returns>Transaction response</returns>
		public virtual async Task<PostTransactionsResponse> DepositAsync(
			Account account,
			ulong stakeAmount,
			bool wait = true) {

			var txs = await PrepareDepositTransactionsAsync(
				account.Address, stakeAmount);

			txs.Sign(account);

			return await Client.SubmitAsync(txs, wait);
		}

		/// <summary>
		/// Withdraw from the stake pool.
		/// </summary>
		/// <param name="account">Account to make withdrawl</param>
		/// <param name="withdrawAmount">Amount to withdraw</param>
		/// <returns></returns>
		public virtual async Task<PostTransactionsResponse> WithdrawAsync(
			Account account,
			ulong withdrawAmount,
			bool wait = true) {

			var txs = await PrepareWithdrawTransactionsAsync(
				account.Address, withdrawAmount);

			txs.Sign(account);

			return await Client.SubmitAsync(txs, wait);
		}

		/// <summary>
		/// Claim reward from stake pool.
		/// </summary>
		/// <param name="account">Account to make claim</param>
		/// <param name="rewardAmount">Amount to claim</param>
		/// <returns>Transaction response</returns>
		public virtual async Task<PostTransactionsResponse> ClaimRewardAsync(
			Account account,
			ulong rewardAmount,
			bool wait = true) {

			var txs = await PrepareClaimRewardTransactionsAsync(
				account.Address, rewardAmount);

			txs.Sign(account);

			return await Client.SubmitAsync(txs, wait);
		}

		/// <summary>
		/// Create a transaction group to opt-in to the pool.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <returns>Transaction group</returns>
		public virtual async Task<TransactionGroup> PrepareOptInTransactionsAsync(Address sender) {

			var txParams = await Client.DefaultApi.ParamsAsync();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolOptInTransactions(ApplicationId, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to opt-in to the stake asset.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <returns>Transaction group</returns>
		public virtual async Task<TransactionGroup> PrepareStakeAssetOptInTransactionsAsync(Address sender) {

			var txParams = await Client.DefaultApi.ParamsAsync();

			var result = YieldlyTransaction
				.PrepareAssetOptInTransactions(StakeAsset.Id, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to opt-in to the reward asset.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <returns>Transaction group</returns>
		public virtual async Task<TransactionGroup> PrepareRewardAssetOptInTransactionsAsync(Address sender) {

			var txParams = await Client.DefaultApi.ParamsAsync();

			var result = YieldlyTransaction
				.PrepareAssetOptInTransactions(RewardAsset.Id, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to opt-out of pool application and, optionally, the reward asset.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="optOutOfRewardAsset">Whether or not to opt-out of the asset</param>
		/// <returns>Transaction group</returns>
		public virtual async Task<TransactionGroup> PrepareOptOutTransactionsAsync(
			Address sender, bool optOutOfRewardAsset = false) {

			var txParams = await Client.DefaultApi.ParamsAsync();

			if (optOutOfRewardAsset) {
				var assetId = RewardAsset.Id;
				var asset = await Client.DefaultApi.AssetsAsync(assetId);
				var closeTo = new Address(asset.Params.Creator);

				return YieldlyTransaction.PrepareAsaStakingPoolOptOutTransactions(
					ApplicationId, assetId, closeTo, sender, txParams);
			}

			return YieldlyTransaction.PrepareAsaStakingPoolOptOutTransactions(
					ApplicationId, sender, txParams);
		}

		/// <summary>
		/// Create a transaction group to deposit into the stake pool
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="stakeAmount">Amount to stake</param>
		/// <returns>Transaction group</returns>
		public virtual async Task<TransactionGroup> PrepareDepositTransactionsAsync(
			Address sender,
			ulong stakeAmount) {

			var txParams = await Client.DefaultApi.ParamsAsync();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolDepositTransactions(stakeAmount, this, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to withdraw from the stake pool
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="withdrawAmount">Amount to withdraw</param>
		/// <returns>Transaction group</returns>
		public virtual async Task<TransactionGroup> PrepareWithdrawTransactionsAsync(
			Address sender,
			ulong withdrawAmount) {

			var txParams = await Client.DefaultApi.ParamsAsync();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolWithdrawTransactions(withdrawAmount, this, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to claim rewards from the stake pool
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="rewardAmount">Amount to claim</param>
		/// <returns>Transaction group</returns>
		public virtual async Task<TransactionGroup> PrepareClaimRewardTransactionsAsync(
			Address sender,
			ulong rewardAmount) {

			var txParams = await Client.DefaultApi.ParamsAsync();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolClaimRewardTransactions(
					rewardAmount, this, sender, txParams);

			return result;
		}

	}

}
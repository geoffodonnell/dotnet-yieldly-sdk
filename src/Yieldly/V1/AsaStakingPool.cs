using Algorand;
using Algorand.V2;
using Algorand.V2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Yieldly.V1.Model;
using Account = Algorand.Account;
using AccountInformation = Algorand.V2.Model.Account;

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
		public void Refresh() {

			Application = Client.AlgodApi.GetApplicationByID((long)ApplicationId);
		}

		/// <summary>
		/// Check whether or not the address is opted-in to the pool application.
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <returns>Whether or not the address is opted-in</returns>
		public virtual bool IsOptedIn(Address address) {

			var info = Client.AlgodApi.AccountInformation(address.EncodeAsString());
			var appId = (long)ApplicationId;

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
		public virtual bool IsOptedInToStakeAsset(Address address) {

			var info = Client.AlgodApi.AccountInformation(address.EncodeAsString());

			return AccountIsOptedInToAsset(info, StakeAsset.Id);
		}

		/// <summary>
		/// Check whether or not the address is opted-in to the reward asset.
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <returns>Whether or not the address is opted-in</returns>
		public virtual bool IsOptedInToRewardAsset(Address address) {
			
			var info = Client.AlgodApi.AccountInformation(address.EncodeAsString());

			return AccountIsOptedInToAsset(info, RewardAsset.Id);
		}

		protected virtual bool AccountIsOptedInToAsset(
			AccountInformation accountInfo, ulong assetId) {

			return accountInfo
				.Assets
				.Any(s => s.AssetId == (long)assetId);
		}

		/// <summary>
		/// Fetch amounts from pool
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <param name="refreshPoolInfo">Whether or not the pool information should be refreshed</param>
		/// <returns>Pool amounts</returns>
		public FetchAsaStakingAmountsResult FetchAmounts(
			Address address, bool refreshPoolInfo = false) {

			var accountInfo = Client.AlgodApi.AccountInformation(address.EncodeAsString());

			if (refreshPoolInfo) {
				Refresh();
			}

			var reward = YieldlyEquation.CalculateAsaStakePoolClaimableAmount(accountInfo, Application);
			var staked = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == (long)ApplicationId)?
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
		public virtual PostTransactionsResponse[] OptIn(
			Account account,
			bool optInToStakeAsset = true,
			bool optInToRewardAsset = true) {

			var results = new List<PostTransactionsResponse>();

			AccountInformation accountInfo = (optInToStakeAsset || optInToRewardAsset)
				? Client.AlgodApi.AccountInformation(account.Address.EncodeAsString())
				: null;

			// If neccessary, opt-in to stake asset
			if (optInToStakeAsset && !AccountIsOptedInToAsset(accountInfo, StakeAsset.Id)) {
				var stakeAssetOptInTxs = PrepareStakeAssetOptInTransactions(account.Address);
				
				stakeAssetOptInTxs.Sign(account);

				var stakeAssetOptInResult = Client.Submit(stakeAssetOptInTxs, true);

				results.Add(stakeAssetOptInResult);
			}

			// If neccessary, opt-in to reward asset
			if (optInToRewardAsset && !AccountIsOptedInToAsset(accountInfo, RewardAsset.Id)) {
				var rewardAssetOptInTxs = PrepareRewardAssetOptInTransactions(account.Address);

				rewardAssetOptInTxs.Sign(account);

				var rewardAssetOptInResult = Client.Submit(rewardAssetOptInTxs, true);

				results.Add(rewardAssetOptInResult);
			}

			// Finally, opt-in to app
			var appOptInTxs = PrepareOptInTransactions(account.Address);

			appOptInTxs.Sign(account);

			var appOptInResult = Client.Submit(appOptInTxs, true);

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
		public virtual PostTransactionsResponse OptOut(
			Account account,
			bool optOutOfRewardAsset = false,
			bool checkRewardAssetBalance = true) {

			var rewardAssetId = (long)RewardAsset.Id;

			if (checkRewardAssetBalance) {
				var accountInfo = Client.AlgodApi
					.AccountInformation(account.Address.EncodeAsString());

				var assetInfo = accountInfo.Assets
					.FirstOrDefault(s => s.AssetId.GetValueOrDefault() == rewardAssetId);

				if (assetInfo?.Amount.GetValueOrDefault() > 0) {
					throw new Exception($"Attempting {RewardAsset.Name} ASA opt-out with non-zero balance.");
				}
			}

			var txs = PrepareOptOutTransactions(
				account.Address, optOutOfRewardAsset);

			txs.Sign(account);

			return Client.Submit(txs, true);
		}

		/// <summary>
		/// Deposit into the stake pool.
		/// </summary>
		/// <param name="account">Account to make deposit</param>
		/// <param name="stakeAmount">Amount to deposit</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse Deposit(
			Account account,
			ulong stakeAmount) {

			var txs = PrepareDepositTransactions(
				account.Address, stakeAmount);

			txs.Sign(account);

			return Client.Submit(txs, true);
		}

		/// <summary>
		/// Withdraw from the stake pool.
		/// </summary>
		/// <param name="account">Account to make withdrawl</param>
		/// <param name="withdrawAmount">Amount to withdraw</param>
		/// <returns></returns>
		public virtual PostTransactionsResponse Withdraw(
			Account account,
			ulong withdrawAmount) {

			var txs = PrepareWithdrawTransactions(
				account.Address, withdrawAmount);

			txs.Sign(account);

			return Client.Submit(txs, true);
		}

		/// <summary>
		/// Claim reward from stake pool.
		/// </summary>
		/// <param name="account">Account to make claim</param>
		/// <param name="rewardAmount">Amount to claim</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse ClaimReward(
			Account account,
			ulong rewardAmount) {

			var txs = PrepareClaimRewardTransactions(
				account.Address, rewardAmount);

			txs.Sign(account);

			return Client.Submit(txs, true);
		}

		/// <summary>
		/// Create a transaction group to opt-in to the pool.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareOptInTransactions(Address sender) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolOptInTransactions(ApplicationId, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to opt-in to the stake asset.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareStakeAssetOptInTransactions(Address sender) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAssetOptInTransactions(StakeAsset.Id, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to opt-in to the reward asset.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareRewardAssetOptInTransactions(Address sender) {

			var txParams = Client.AlgodApi.TransactionParams();

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
		public virtual TransactionGroup PrepareOptOutTransactions(
			Address sender, bool optOutOfRewardAsset = false) {

			var txParams = Client.AlgodApi.TransactionParams();

			if (optOutOfRewardAsset) {
				var assetId = RewardAsset.Id;
				var asset = Client.AlgodApi.GetAssetByID((long)assetId);
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
		public virtual TransactionGroup PrepareDepositTransactions(
			Address sender,
			ulong stakeAmount) {

			var txParams = Client.AlgodApi.TransactionParams();

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
		public virtual TransactionGroup PrepareWithdrawTransactions(
			Address sender,
			ulong withdrawAmount) {

			var txParams = Client.AlgodApi.TransactionParams();

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
		public virtual TransactionGroup PrepareClaimRewardTransactions(
			Address sender,
			ulong rewardAmount) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolClaimRewardTransactions(
					rewardAmount, this, sender, txParams);

			return result;
		}

	}

}
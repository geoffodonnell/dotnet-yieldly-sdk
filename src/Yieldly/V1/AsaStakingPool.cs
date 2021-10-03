using Algorand;
using Algorand.V2;
using Algorand.V2.Model;
using System;
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

		public void Refresh() {

			Application = Client.AlgodApi.GetApplicationByID((long)ApplicationId);
		}

		/// <summary>
		/// Check whether or not the address is opted in to the pool application.
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <returns>Whether or not the address is opted in</returns>
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

		public virtual bool IsOptedInToStakeAsset(Address address) {

			var info = Client.AlgodApi.AccountInformation(address.EncodeAsString());

			return AccountIsOptedInToAsset(info, StakeAsset.Id);
		}

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

		public FetchAsaStakingPoolAmountsResult FetchAmounts(
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

			return new FetchAsaStakingPoolAmountsResult {
				Reward = reward?.Asa ?? 0ul,
				Staked = staked.GetValueOrDefault()
			};
		}

		public virtual PostTransactionsResponse OptIn(
			Account account,
			bool optInToStakeAsset = true,
			bool optInToRewardAsset = true) {

			AccountInformation accountInfo = (optInToStakeAsset || optInToRewardAsset)
				? Client.AlgodApi.AccountInformation(account.Address.EncodeAsString())
				: null;

			// If neccessary, opt-in to stake asset
			if (optInToStakeAsset && !AccountIsOptedInToAsset(accountInfo, StakeAsset.Id)) {
				var stakeAssetOptInTxs = PrepareStakeAssetOptInTransactions(account.Address);
				
				stakeAssetOptInTxs.Sign(account);

				Client.Submit(stakeAssetOptInTxs, true);
			}

			// If neccessary, opt-in to reward asset
			if (optInToRewardAsset && !AccountIsOptedInToAsset(accountInfo, RewardAsset.Id)) {
				var rewardAssetOptInTxs = PrepareRewardAssetOptInTransactions(account.Address);

				rewardAssetOptInTxs.Sign(account);

				Client.Submit(rewardAssetOptInTxs, true);
			}

			// Finally, opt-in to app
			var appOptInTxs = PrepareOptInTransactions(account.Address);

			appOptInTxs.Sign(account);

			return Client.Submit(appOptInTxs, true);
		}

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

		public virtual PostTransactionsResponse Deposit(
			Account account,
			ulong stakeAmount) {

			var txs = PrepareDepositTransactions(
				account.Address, stakeAmount);

			txs.Sign(account);

			return Client.Submit(txs, true);
		}

		public virtual PostTransactionsResponse Withdraw(
			Account account,
			ulong stakeAmount) {

			var txs = PrepareWithdrawTransactions(
				account.Address, stakeAmount);

			txs.Sign(account);

			return Client.Submit(txs, true);
		}

		public virtual PostTransactionsResponse ClaimReward(
			Account account,
			ulong rewardAmount) {

			var txs = PrepareClaimRewardTransactions(
				account.Address, rewardAmount);

			txs.Sign(account);

			return Client.Submit(txs, true);
		}
		
		public virtual TransactionGroup PrepareOptInTransactions(Address sender) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolOptInTransactions(ApplicationId, sender, txParams);

			return result;
		}

		public virtual TransactionGroup PrepareStakeAssetOptInTransactions(Address sender) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAssetOptInTransactions(StakeAsset.Id, sender, txParams);

			return result;
		}

		public virtual TransactionGroup PrepareRewardAssetOptInTransactions(Address sender) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAssetOptInTransactions(RewardAsset.Id, sender, txParams);

			return result;
		}

		/// <summary>
		/// Opt-out of pool application and, optionally, the reward asset.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="optOutOfRewardAsset">Whether or not to opt-out of the asset</param>
		/// <returns>Transactions that perfom this action</returns>
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

		public virtual TransactionGroup PrepareDepositTransactions(
			Address sender,
			ulong stakeAmount) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolDepositTransactions(stakeAmount, this, sender, txParams);

			return result;
		}

		public virtual TransactionGroup PrepareWithdrawTransactions(
			Address sender,
			ulong stakeAmount) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolWithdrawTransactions(stakeAmount, this, sender, txParams);

			return result;
		}

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
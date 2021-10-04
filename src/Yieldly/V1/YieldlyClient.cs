using Algorand;
using Algorand.V2;
using Algorand.V2.Model;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Linq;
using Yieldly.V1.Model;
using Account = Algorand.Account;

namespace Yieldly.V1 {

	public class YieldlyClient {

		private readonly AlgodApi mAlgodApi;

		internal AlgodApi AlgodApi { get => mAlgodApi; }

		/// <summary>
		/// Construct a new instance which connects to the default node
		/// </summary>
		public YieldlyClient() :
			this(new AlgodApi(Constant.AlgodMainnetHost, String.Empty)) { }

		/// <summary>
		/// Construct a new instance
		/// </summary>
		/// <param name="algodApi">Algod API connection</param>
		public YieldlyClient(
			AlgodApi algodApi) {

			mAlgodApi = algodApi;
		}

		/// <summary>
		/// Submit a signed transaction group.
		/// </summary>
		/// <param name="transactionGroup">Signed transaction group</param>
		/// <param name="wait">Wait for confirmation</param>
		/// <returns>Transaction reponse</returns>
		public virtual PostTransactionsResponse Submit(
			TransactionGroup transactionGroup, bool wait = true) {

			return transactionGroup.Submit(mAlgodApi, wait);
		}

		/// <summary>
		/// Fetch amounts from Lottery and Yieldly staking apps.
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <returns>Application amounts</returns>
		public virtual FetchAmountsResult FetchAmounts(Address address) {

			var accountInfo = mAlgodApi.AccountInformation(address.EncodeAsString());
			var lotteryApp = mAlgodApi.GetApplicationByID((long)Constant.LotteryAppId);
			var stakingApp = mAlgodApi.GetApplicationByID((long)Constant.StakingAppId);

			var lotteryReward = YieldlyEquation.CalculateLotteryClaimableAmount(accountInfo, lotteryApp);
			var stakingReward = YieldlyEquation.CalculateStakingClaimableAmount(accountInfo, stakingApp);
			var algoInLottery = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == lotteryApp.Id)?
				.KeyValue?
				.GetUserAmountValue();
			var yieldlyStaked = accountInfo?
				.AppsLocalState?
				.FirstOrDefault(s => s.Id == stakingApp.Id)?
				.KeyValue?
				.GetUserAmountValue()
				.GetValueOrDefault();

			return new FetchAmountsResult { 
				LotteryReward = lotteryReward,
				StakingReward = stakingReward,
				AlgoInLottery = algoInLottery.GetValueOrDefault(),
				YieldlyStaked = yieldlyStaked.GetValueOrDefault()
			};
		}

		/// <summary>
		/// Get a stake pool by application ID.
		/// </summary>
		/// <param name="appId">Application ID of the staking pool</param>
		/// <returns>Stake pool</returns>
		public virtual AsaStakingPool FetchStakingPool(ulong appId) {

			var lsigSignature = Contract.GetAsaStakePoolLogicsigSignature(appId);
			var poolApp = mAlgodApi.GetApplicationByID((long)appId);
			var globalState = poolApp.Params.GlobalState
				.ToDictionary(s => YieldlyUtils.Base64Decode(s.Key), s => s.Value);

			Address escrowAddress = null;

			if (globalState.TryGetValue("E", out var value)) {
				escrowAddress = new Address(Base64.Decode(value?.Bytes));
			} else {
				return null;
			}

			ulong? stakeAssetId = null;

			if (globalState.TryGetValue("SA", out value)) {
				stakeAssetId = value.Uint;
			} else {
				return null;
			}

			var stakeAsset = mAlgodApi.GetAssetByID((long)stakeAssetId);

			ulong? rewardAssetId = null;

			if (globalState.TryGetValue("RA", out value)) {
				rewardAssetId = value.Uint;
			} else { 
				return null;
			}

			var rewardAsset = mAlgodApi.GetAssetByID((long)rewardAssetId);

			return new AsaStakingPool { 
				Client = this,
				ApplicationId = appId,
				Address = escrowAddress.EncodeAsString(),
				StakeAsset = stakeAsset.ToSimpleAsset(),
				RewardAsset = rewardAsset.ToSimpleAsset(),
				Application = poolApp,
				LogicsigSignature = lsigSignature
			};
		}

		/// <summary>
		/// Opt-in to Yieldly base contracts and asset.
		/// </summary>
		/// <param name="account">Account to opt-in</param>
		/// <param name="includeProxyContract">Whether or not to opt-in to the proxy contract</param>
		/// <param name="includeStakingContract">Whether or not to opt-in to the staking contract</param>
		/// <param name="includeLotteryContract">Whether or not to opt-in to the lottery contract</param>
		/// <param name="includeYieldlyAsa">Whether or not to opt-in to the Yieldly asset</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse OptIn(
			Account account,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true,
			bool includeYieldlyAsa = true) {

			var txs = PrepareOptInTransactions(
				account.Address,
				includeYieldlyAsa,
				includeProxyContract,
				includeStakingContract,
				includeLotteryContract);

			txs.Sign(account);

			return Submit(txs, true);
		}

		/// <summary>
		/// Opt-out of Yieldly base contracts and asset.
		/// </summary>
		/// <param name="account">Account to opt-out</param>
		/// <param name="includeProxyContract">Whether or not to opt-out of the proxy contract</param>
		/// <param name="includeStakingContract">Whether or not to opt-out of the staking contract</param>
		/// <param name="includeLotteryContract">Whether or not to opt-out of the lottery contract</param>
		/// <param name="includeYieldlyAsa">Whether or not to opt-out of the Yieldly asset</param>
		/// <param name="checkAssetBalance">Whether or not to check the balance before opting-out</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse OptOut(
			Account account,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true,
			bool includeYieldlyAsa = false,
			bool checkAssetBalance = true) {

			var yieldlyAssetId = (long)Constant.YieldlyAssetId;

			if (checkAssetBalance) {
				var accountInfo = mAlgodApi
					.AccountInformation(account.Address.EncodeAsString());

				var assetInfo = accountInfo.Assets
					.FirstOrDefault(s => s.AssetId.GetValueOrDefault() == yieldlyAssetId);

				if (assetInfo?.Amount.GetValueOrDefault() > 0) {
					throw new Exception("Attempting Yieldly ASA opt-out with non-zero balance.");
				}
			}

			var txs = PrepareOptOutTransactions(
				account.Address,
				includeYieldlyAsa,
				includeProxyContract,
				includeStakingContract,
				includeLotteryContract);

			txs.Sign(account);

			return Submit(txs, true);
		}

		/// <summary>
		/// Deposit into the lottery.
		/// </summary>
		/// <param name="account">Account to make deposit</param>
		/// <param name="algoAmount">Amount to deposit</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse LotteryDeposit(
			Account account,
			ulong algoAmount) {

			var txs = PrepareLotteryDepositTransactions(
				account.Address, algoAmount);

			txs.Sign(account);

			return Submit(txs, true);
		}

		/// <summary>
		/// Withdraw from the lottery.
		/// </summary>
		/// <param name="account">Account to make withdrawl</param>
		/// <param name="algoAmount">Amount to withdraw</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse LotteryWithdraw(
			Account account,
			ulong algoAmount) {

			var txs = PrepareLotteryWithdrawTransactions(
				account.Address, algoAmount);

			txs.Sign(account);

			return Submit(txs, true);
		}

		/// <summary>
		/// Claim reward from lottery.
		/// </summary>
		/// <param name="account">Account to make claim</param>
		/// <param name="amount">Amount to claim</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse LotteryClaimReward(
			Account account,
			LotteryRewardAmount amount) {

			var txs = PrepareLotteryClaimRewardTransactions(
				account.Address, amount);

			txs.Sign(account);

			return Submit(txs, true);
		}

		/// <summary>
		/// Deposit into the Yieldly staking pool.
		/// </summary>
		/// <param name="account">Account to make deposit</param>
		/// <param name="yieldlyAmount">Amount to deposit</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse YieldlyStakingDeposit(
			Account account,
			ulong yieldlyAmount) {

			var txs = PrepareYieldlyStakingDepositTransactions(
				account.Address, yieldlyAmount);

			txs.Sign(account);

			return Submit(txs, true);
		}

		/// <summary>
		/// Withdraw from the Yieldly staking pool.
		/// </summary>
		/// <param name="account">Account to make withdrawl</param>
		/// <param name="yieldlyAmount">Amount to withdraw</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse YieldlyStakingWithdraw(
			Account account,
			ulong yieldlyAmount) {

			var txs = PrepareYieldlyStakingWithdrawTransactions(
				account.Address, yieldlyAmount);

			txs.Sign(account);

			return Submit(txs, true);
		}

		/// <summary>
		/// Claim reward from the Yieldly staking pool.
		/// </summary>
		/// <param name="account">Account to make claim</param>
		/// <param name="amount">Amount to claim</param>
		/// <returns>Transaction response</returns>
		public virtual PostTransactionsResponse YieldyStakingClaimReward(
			Account account,
			StakingRewardAmount amount) {

			var txs = PrepareYieldlyStakingClaimRewardTransactions(
				account.Address, amount);

			txs.Sign(account);

			return Submit(txs, true);
		}

		/// <summary>
		/// Create a transaction group to opt-in to the base contracts and Yieldly asset.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="includeProxyContract">Whether or not to opt-in to the proxy contract</param>
		/// <param name="includeStakingContract">Whether or not to opt-in to the staking contract</param>
		/// <param name="includeLotteryContract">Whether or not to opt-in to the lottery contract</param>
		/// <param name="includeYieldlyAsa">Whether or not to opt-in to Yieldly asset</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareOptInTransactions(
			Address sender,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true,
			bool includeYieldlyAsa = true) {

			var txParams = mAlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareOptInTransactions(
					sender,
					txParams,
					includeYieldlyAsa,
					includeProxyContract,
					includeStakingContract,
					includeLotteryContract);

			return result;
		}

		/// <summary>
		/// Create a transaction group to opt-out of the base contracts and Yieldly asset.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="includeProxyContract">Whether or not to opt-out of the proxy contract</param>
		/// <param name="includeStakingContract">Whether or not to opt-of the staking contract</param>
		/// <param name="includeLotteryContract">Whether or not to opt-out of the lottery contract</param>
		/// <param name="includeYieldlyAsa">Whether or not to opt-out of the Yieldly asset</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareOptOutTransactions(
			Address sender,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true,
			bool includeYieldlyAsa = true) {

			var txParams = mAlgodApi.TransactionParams();
			var asset = mAlgodApi.GetAssetByID((long)Constant.YieldlyAssetId);

			var closeTo = new Address(asset.Params.Creator);

			var result = YieldlyTransaction
				.PrepareOptOutTransactions(
					closeTo,
					sender,
					txParams,
					includeYieldlyAsa,
					includeProxyContract,
					includeStakingContract,
					includeLotteryContract);

			return result;
		}

		/// <summary>
		/// Create a transaction group to deposit into the lottery.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="algoAmount">Amount to deposit</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareLotteryDepositTransactions(
			Address sender,
			ulong algoAmount) {

			var txParams = mAlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareLotteryDepositTransactions(algoAmount, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to withdraw from the lottery.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="algoAmount">Amount to withdraw</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareLotteryWithdrawTransactions(
			Address sender,
			ulong algoAmount) {

			var txParams = mAlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareLotteryWithdrawTransactions(algoAmount, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to claim rewards from the lottery.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="amount">Amount to claim</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareLotteryClaimRewardTransactions(
			Address sender,
			LotteryRewardAmount amount) {

			var txParams = mAlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareLotteryClaimRewardTransactions(
					amount.Yieldly, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to deposit into the Yieldly staking pool.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="yieldlyAmount">Amount to deposit</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareYieldlyStakingDepositTransactions(
			Address sender,
			ulong yieldlyAmount) {

			var txParams = mAlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareYieldlyStakingDepositTransactions(yieldlyAmount, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to withdraw from the Yieldly staking pool.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="yieldlyAmount">Amount to withdraw</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareYieldlyStakingWithdrawTransactions(
			Address sender,
			ulong yieldlyAmount) {

			var txParams = mAlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareYieldlyStakingWithdrawTransactions(yieldlyAmount, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to claim rewards from the Yieldly staking pool.
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="amount">Amount to claim</param>
		/// <returns>Transaction group</returns>
		public virtual TransactionGroup PrepareYieldlyStakingClaimRewardTransactions(
			Address sender,
			StakingRewardAmount amount) {

			var txParams = mAlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareYieldlyStakingClaimRewardTransactions(
					amount.Algo, amount.Yieldly, sender, txParams);

			return result;
		}

	}

}

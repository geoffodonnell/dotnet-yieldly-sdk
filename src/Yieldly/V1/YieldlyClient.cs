using Algorand;
using Algorand.Common;
using Algorand.V2;
using Algorand.V2.Algod;
using Algorand.V2.Algod.Model;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Yieldly.V1.Model;
using Account = Algorand.Account;

namespace Yieldly.V1 {

	public class YieldlyClient {

		protected readonly IDefaultApi mDefaultApi;
		protected readonly HttpClient mHttpClient;
		protected readonly ConcurrentDictionary<ulong, SimpleAsset> mAssetCache;

		public IDefaultApi DefaultApi { get => mDefaultApi; }

		/// <summary>
		/// Construct a new instance which connects to the default node
		/// </summary>
		public YieldlyClient() :
			this(Constant.AlgodMainnetHost, String.Empty) { }

		/// <summary>
		/// Construct a new instance with an existing Algod API service
		/// </summary>
		/// <param name="defaultApi">Algod API service</param>
		public YieldlyClient(IDefaultApi defaultApi) {

			mHttpClient = null;
			mDefaultApi = defaultApi;
			mAssetCache = new ConcurrentDictionary<ulong, SimpleAsset>();

		}

		/// <summary>
		/// Construct a new instance with a URL and token
		/// </summary>
		/// <param name="url">Algorand node URL</param>
		/// <param name="token">Algorand node API key</param>
		/// <remarks>
		/// This constructor creates an instance of <see cref="HttpClient"/>
		/// </remarks>
		public YieldlyClient(string url, string token) {

			mHttpClient = HttpClientConfigurator.ConfigureHttpClient(url, token);
			mDefaultApi = new DefaultApi(mHttpClient) {
				BaseUrl = url
			};
			mAssetCache = new ConcurrentDictionary<ulong, SimpleAsset>();
		}

		/// <summary>
		/// Construct a new instance
		/// </summary>
		/// <param name="httpClient">HttpClient instance</param>
		/// <param name="url">Algorand node URL</param>
		public YieldlyClient(
			HttpClient httpClient, string url) {

			mHttpClient = httpClient;
			mDefaultApi = new DefaultApi(mHttpClient) {
				BaseUrl = url
			};
		}

		/// <summary>
		/// Retrieve the current network parameters.
		/// </summary>
		/// <returns>Current network parameters</returns>
		public virtual async Task<TransactionParametersResponse> FetchTransactionParamsAsync() {

			return await mDefaultApi.ParamsAsync();
		}

		/// <summary>
		/// Fetch an asset given the asset ID.
		/// </summary>
		/// <param name="id">The asset ID</param>
		/// <returns>The asset</returns>
		public virtual async Task<SimpleAsset> FetchAssetAsync(ulong id) {

			if (mAssetCache.TryGetValue(id, out var value)) {
				return value;
			}

			value = await FetchAssetFromApiAsync(id);

			return mAssetCache.GetOrAdd(id, s => value);
		}

		/// <summary>
		/// Submit a signed transaction group.
		/// </summary>
		/// <param name="transactionGroup">Signed transaction group</param>
		/// <param name="wait">Wait for confirmation</param>
		/// <returns>Transaction reponse</returns>
		public virtual async Task<PostTransactionsResponse> SubmitAsync(
			TransactionGroup transactionGroup, bool wait = true) {

			return await mDefaultApi.SubmitTransactionGroupAsync(transactionGroup, wait);
		}

		/// <summary>
		/// Fetch amounts from Lottery and Yieldly staking apps.
		/// </summary>
		/// <param name="address">Address of account</param>
		/// <returns>Application amounts</returns>
		public virtual async Task<FetchAmountsResult> FetchAmountsAsync(Address address) {

			var accountInfo = await mDefaultApi.AccountsAsync(address.EncodeAsString(), Format.Json);
			var lotteryApp = await mDefaultApi.ApplicationsAsync((int)Constant.LotteryAppId);
			var stakingApp = await mDefaultApi.ApplicationsAsync((int)Constant.StakingAppId);

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
		public virtual async Task<AsaStakingPool> FetchStakingPoolAsync(ulong appId) {

			var application = await mDefaultApi.ApplicationsAsync((int)appId);

			if (!String.IsNullOrWhiteSpace(ApplicationState.GetBytes(application.Params.GlobalState, "E"))) {
				return await CreateTeal3StakingPool(application);
			} else {
				return await CreateTeal5StakingPool(application);
			}
		}

		protected virtual async Task<AsaStakingPool> CreateTeal3StakingPool(Application application) {

			var lsigSignature = Contract.GetAsaStakePoolLogicsigSignature(application.Id);
			var globalState = application.Params.GlobalState
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

			var stakeAsset = await FetchAssetAsync(stakeAssetId.Value);

			ulong? rewardAssetId = null;

			if (globalState.TryGetValue("RA", out value)) {
				rewardAssetId = value.Uint;
			} else {
				return null;
			}

			var rewardAsset = await FetchAssetAsync(rewardAssetId.Value);

			return new AsaStakingPool {
				Client = this,
				ApplicationId = application.Id,
				Address = escrowAddress.EncodeAsString(),
				StakeAsset = stakeAsset,
				RewardAsset = rewardAsset,
				Application = application,
				LogicsigSignature = lsigSignature
			};
		}

		protected virtual async Task<AsaStakingPool> CreateTeal5StakingPool(Application application) {

			var address = Algorand.Address.ForApplication(application.Id);
			var precision = YieldlyUtils.GetBigInteger(application.Params.GlobalState, "Precision");
			var stakeAssetId = ApplicationState.GetNumber(application.Params.GlobalState, "Staking_Token");
			var rewardAssetId = ApplicationState.GetNumber(application.Params.GlobalState, "Reward_Token_1");

			var stakeAsset = await FetchAssetAsync(stakeAssetId.Value);
			var rewardAsset = await FetchAssetAsync(rewardAssetId.Value);

			return new AsaStakingPoolTeal5 {
				Client = this,
				ApplicationId = application.Id,
				Address = address.EncodeAsString(),
				StakeAsset = stakeAsset,
				RewardAsset = rewardAsset,
				Application = application,
				LogicsigSignature = null,
				Precision = precision
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
		public virtual async Task<PostTransactionsResponse> OptInAsync(
			Account account,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true,
			bool includeYieldlyAsa = true) {

			var txs = await PrepareOptInTransactionsAsync(
				account.Address,
				includeYieldlyAsa,
				includeProxyContract,
				includeStakingContract,
				includeLotteryContract);

			txs.Sign(account);

			return await SubmitAsync(txs, true);
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
		public virtual async Task<PostTransactionsResponse> OptOutAsync(
			Account account,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true,
			bool includeYieldlyAsa = false,
			bool checkAssetBalance = true) {

			var yieldlyAssetId = Constant.YieldlyAssetId;

			if (checkAssetBalance) {
				var accountInfo = await mDefaultApi
					.AccountsAsync(account.Address.EncodeAsString(), Format.Json);

				var assetInfo = accountInfo.Assets
					.FirstOrDefault(s => s.AssetId == yieldlyAssetId);

				if (assetInfo == null) {
					throw new Exception("Attempting Yieldly ASA opt-out; account has not opted-in to Yieldly ASA.");
				}

				if (assetInfo.Amount > 0) {
					throw new Exception("Attempting Yieldly ASA opt-out with non-zero balance.");
				}
			}

			var txs = await PrepareOptOutTransactionsAsync(
				account.Address,
				includeYieldlyAsa,
				includeProxyContract,
				includeStakingContract,
				includeLotteryContract);

			txs.Sign(account);

			return await SubmitAsync(txs, true);
		}

		/// <summary>
		/// Deposit into the lottery.
		/// </summary>
		/// <param name="account">Account to make deposit</param>
		/// <param name="algoAmount">Amount to deposit</param>
		/// <returns>Transaction response</returns>
		public virtual async Task<PostTransactionsResponse> LotteryDepositAsync(
			Account account,
			ulong algoAmount) {

			var txs = await PrepareLotteryDepositTransactionsAsync(
				account.Address, algoAmount);

			txs.Sign(account);

			return await SubmitAsync(txs, true);
		}

		/// <summary>
		/// Withdraw from the lottery.
		/// </summary>
		/// <param name="account">Account to make withdrawl</param>
		/// <param name="algoAmount">Amount to withdraw</param>
		/// <returns>Transaction response</returns>
		public virtual async Task<PostTransactionsResponse> LotteryWithdrawAsync(
			Account account,
			ulong algoAmount) {

			var txs = await PrepareLotteryWithdrawTransactionsAsync(
				account.Address, algoAmount);

			txs.Sign(account);

			return await SubmitAsync(txs, true);
		}

		/// <summary>
		/// Claim reward from lottery.
		/// </summary>
		/// <param name="account">Account to make claim</param>
		/// <param name="amount">Amount to claim</param>
		/// <returns>Transaction response</returns>
		public virtual async Task<PostTransactionsResponse> LotteryClaimRewardAsync(
			Account account,
			LotteryRewardAmount amount) {

			var txs = await PrepareLotteryClaimRewardTransactionsAsync(
				account.Address, amount);

			txs.Sign(account);

			return await SubmitAsync(txs, true);
		}

		/// <summary>
		/// Deposit into the Yieldly staking pool.
		/// </summary>
		/// <param name="account">Account to make deposit</param>
		/// <param name="yieldlyAmount">Amount to deposit</param>
		/// <returns>Transaction response</returns>
		public virtual async Task<PostTransactionsResponse> YieldlyStakingDepositAsync(
			Account account,
			ulong yieldlyAmount) {

			var txs = await PrepareYieldlyStakingDepositTransactionsAsync(
				account.Address, yieldlyAmount);

			txs.Sign(account);

			return await SubmitAsync(txs, true);
		}

		/// <summary>
		/// Withdraw from the Yieldly staking pool.
		/// </summary>
		/// <param name="account">Account to make withdrawl</param>
		/// <param name="yieldlyAmount">Amount to withdraw</param>
		/// <returns>Transaction response</returns>
		public virtual async Task<PostTransactionsResponse> YieldlyStakingWithdrawAsync(
			Account account,
			ulong yieldlyAmount) {

			var txs = await PrepareYieldlyStakingWithdrawTransactionsAsync(
				account.Address, yieldlyAmount);

			txs.Sign(account);

			return await SubmitAsync(txs, true);
		}

		/// <summary>
		/// Claim reward from the Yieldly staking pool.
		/// </summary>
		/// <param name="account">Account to make claim</param>
		/// <param name="amount">Amount to claim</param>
		/// <returns>Transaction response</returns>
		public virtual async Task<PostTransactionsResponse> YieldyStakingClaimRewardAsync(
			Account account,
			StakingRewardAmount amount) {

			var txs = await PrepareYieldlyStakingClaimRewardTransactionsAsync(
				account.Address, amount);

			txs.Sign(account);

			return await SubmitAsync(txs, true);
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
		public virtual async Task<TransactionGroup> PrepareOptInTransactionsAsync(
			Address sender,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true,
			bool includeYieldlyAsa = true) {

			var txParams = await mDefaultApi.ParamsAsync();

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
		public virtual async Task<TransactionGroup> PrepareOptOutTransactionsAsync(
			Address sender,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true,
			bool includeYieldlyAsa = true) {

			var txParams = await mDefaultApi.ParamsAsync();
			var asset = await FetchAssetAsync(Constant.YieldlyAssetId);

			var closeTo = new Address(asset.Creator);

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
		public virtual async Task<TransactionGroup> PrepareLotteryDepositTransactionsAsync(
			Address sender,
			ulong algoAmount) {

			var txParams = await mDefaultApi.ParamsAsync();

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
		public virtual async Task<TransactionGroup> PrepareLotteryWithdrawTransactionsAsync(
			Address sender,
			ulong algoAmount) {

			var txParams = await mDefaultApi.ParamsAsync();

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
		public virtual async Task<TransactionGroup> PrepareLotteryClaimRewardTransactionsAsync(
			Address sender,
			LotteryRewardAmount amount) {

			var txParams = await mDefaultApi.ParamsAsync();

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
		public virtual async Task<TransactionGroup> PrepareYieldlyStakingDepositTransactionsAsync(
			Address sender,
			ulong yieldlyAmount) {

			var txParams = await mDefaultApi.ParamsAsync();

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
		public virtual async Task<TransactionGroup> PrepareYieldlyStakingWithdrawTransactionsAsync(
			Address sender,
			ulong yieldlyAmount) {

			var txParams = await mDefaultApi.ParamsAsync();

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
		public virtual async Task<TransactionGroup> PrepareYieldlyStakingClaimRewardTransactionsAsync(
			Address sender,
			StakingRewardAmount amount) {

			var txParams = await mDefaultApi.ParamsAsync();

			var result = YieldlyTransaction
				.PrepareYieldlyStakingClaimRewardTransactions(
					amount.Algo, amount.Yieldly, sender, txParams);

			return result;
		}

		protected virtual async Task<SimpleAsset> FetchAssetFromApiAsync(ulong id) {

			if (id == 0) {
				return new SimpleAsset {
					Id = 0,
					Name = "Algo",
					UnitName = "ALGO",
					Decimals = 6,
					Creator = null
				};
			}

			var asset = await DefaultApi.AssetsAsync(id);

			return new SimpleAsset {
				Id = id,
				Name = asset.Params.Name,
				UnitName = asset.Params.UnitName,
				Decimals = asset.Params.Decimals,
				Creator = asset.Params.Creator
			};
		}

	}

}

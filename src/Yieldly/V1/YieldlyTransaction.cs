using Algorand;
using Algorand.Common;
using Algorand.V2.Algod.Model;
using Algorand.V2.Indexer.Model;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using Transaction = Algorand.Transaction;

namespace Yieldly.V1 {

	public static class YieldlyTransaction {

		#region Opt In/Out

		/// <summary>
		/// Prepare a transaction group to opt-in to contracts and the Yieldly ASA
		/// </summary>
		/// <param name="sender">Account address</param>
		/// <param name="txParams">Network parameters</param>
		/// <param name="includeYieldlyAsa">Whether or not to opt-in to the Yieldly ASA</param>
		/// <param name="includeProxyContract">Whether or not to opt-in to the proxy contract</param>
		/// <param name="includeStakingContract">Whether or not to opt-in to the staking contract</param>
		/// <param name="includeLotteryContract">Whether or not to opt-in to the lottery contract</param>
		/// <returns>Transaction group to execute action</returns>
		public static TransactionGroup PrepareOptInTransactions(
			Address sender,
			TransactionParametersResponse txParams,
			bool includeYieldlyAsa = true,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true) {

			var transactions = new List<Transaction>();

			if (includeYieldlyAsa) {
				transactions.Add(Algorand.Utils.GetAssetOptingInTransaction(
					sender, (long)Constant.YieldlyAssetId, txParams));
			}

			if (includeProxyContract) {
				transactions.Add(Algorand.Utils.GetApplicationOptinTransaction(
					sender, (long)Constant.ProxyAppId, txParams));
			}

			if (includeStakingContract) {
				transactions.Add(Algorand.Utils.GetApplicationOptinTransaction(
					sender, (long)Constant.StakingAppId, txParams));
			}

			if (includeLotteryContract) {
				transactions.Add(Algorand.Utils.GetApplicationOptinTransaction(
					sender, (long)Constant.LotteryAppId, txParams));
			}

			return new TransactionGroup(transactions);
		}

		/// <summary>
		/// Prepare a transaction group to opt-in to staking pool.
		/// </summary>
		/// <param name="appId">Pool application ID</param>
		/// <param name="sender">Account address</param>
		/// <param name="txParams">Network parameters</param>
		/// <returns>Transaction group to execute action</returns>
		public static TransactionGroup PrepareAsaStakingPoolOptInTransactions(
			ulong appId,
			Address sender,
			TransactionParametersResponse txParams) {

			// NOTE: Sending a tx group containing app opt-in and asset opt-in transactions
			// fails, so the opt-in process is a two step process.

			var transactions = new List<Transaction>() {
				Algorand.Utils.GetApplicationOptinTransaction(sender, appId, txParams)
			};

			return new TransactionGroup(transactions);
		}

		/// <summary>
		/// Prepare a transaction group to opt-out of contracts and the Yieldly ASA
		/// </summary>
		/// <param name="closeTo">Address to send remaining Yieldly</param>
		/// <param name="sender">Account address</param>
		/// <param name="txParams">Network parameters</param>
		/// <param name="includeYieldlyAsa">Whether or not to opt-out of the Yieldly ASA</param>
		/// <param name="includeProxyContract">Whether or not to opt-out of the proxy contract</param>
		/// <param name="includeStakingContract">Whether or not to opt-out of the staking contract</param>
		/// <param name="includeLotteryContract">Whether or not to opt-out of the lottery contract</param>
		/// <returns>Transaction group to execute action</returns>
		public static TransactionGroup PrepareOptOutTransactions(
			Address closeTo,
			Address sender,
			TransactionParametersResponse txParams,
			bool includeYieldlyAsa = true,
			bool includeProxyContract = true,
			bool includeStakingContract = true,
			bool includeLotteryContract = true) {

			var transactions = new List<Transaction>();

			if (includeYieldlyAsa) {
				transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
					sender, sender, (long)Constant.YieldlyAssetId, 0, txParams, closeTo: closeTo));
			}

			if (includeProxyContract) {
				transactions.Add(Algorand.Utils.GetApplicationCloseTransaction(
					sender, (long)Constant.ProxyAppId, txParams));
			}

			if (includeStakingContract) {
				transactions.Add(Algorand.Utils.GetApplicationCloseTransaction(
					sender, (long)Constant.StakingAppId, txParams));
			}

			if (includeLotteryContract) {
				transactions.Add(Algorand.Utils.GetApplicationCloseTransaction(
					sender, (long)Constant.LotteryAppId, txParams));
			}

			return new TransactionGroup(transactions);
		}

		/// <summary>
		/// Prepare a transaction group to opt-out of staking pool
		/// </summary>
		/// <param name="appId">Pool application ID</param>
		/// <param name="sender">Account address</param>
		/// <param name="txParams">Network parameters</param>
		/// <returns>Transaction group to execute action</returns>
		public static TransactionGroup PrepareAsaStakingPoolOptOutTransactions(
			ulong appId,
			Address sender,
			TransactionParametersResponse txParams) {

			return PrepareAsaStakingPoolOptOutTransactions(appId, null, sender, null, txParams);
		}

		/// <summary>
		/// Prepare a transaction group to opt-out of staking pool and, optionally, an asset.
		/// </summary>
		/// <param name="appId">Pool application ID</param>
		/// <param name="assetId">Asset ID (optional)</param>
		/// <param name="closeTo">Account to send remaining amount to (if any)</param>
		/// <param name="sender">Account address</param>
		/// <param name="txParams">Network parameters</param>
		/// <returns>Transaction group to execute action</returns>
		public static TransactionGroup PrepareAsaStakingPoolOptOutTransactions(
			ulong appId,
			ulong? assetId,
			Address closeTo,
			Address sender,
			TransactionParametersResponse txParams) {

			var transactions = new List<Transaction>();

			transactions.Add(Algorand.Utils.GetApplicationClearTransaction(
				sender, appId, txParams));

			if (assetId.HasValue) {
				if (closeTo == null) {
					throw new ArgumentException(
						$"'{nameof(closeTo)}' cannot be null when '{nameof(assetId)}' is defined.");
				}

				transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
					sender, sender, assetId, 0, txParams, closeTo: closeTo));
			}

			return new TransactionGroup(transactions);
		}
		#endregion

		#region Lottery Transactions

		public static TransactionGroup PrepareLotteryDepositTransactions(
			ulong algoAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowAddress = Contract.EscrowLogicsigSignature.Address;
			var transactions = new List<Transaction>();

			// Call Proxy App w/ arg check
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.ProxyAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx1);

			// Call No Loss Lottery App w/ arg D
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.LotteryAppId, txParams);

			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("D"));

			transactions.Add(callTx2);

			// Deposit
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
					sender, escrowAddress, algoAmount, null, txParams));

			return new TransactionGroup(transactions);
		}

		public static TransactionGroup PrepareLotteryWithdrawTransactions(
			ulong algoAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = Contract.EscrowLogicsigSignature;
			var escrowAddress = escrowSignature.Address;
			var transactions = new List<Transaction>();

			// Call Proxy App w/ arg check
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.ProxyAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx1);

			// Call No Loss Lottery App w/ arg W
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.LotteryAppId, txParams);

			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("W"));
			callTx2.accounts.Add(escrowAddress);

			transactions.Add(callTx2);

			// Withdrawl
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
					escrowAddress, sender, algoAmount, null, txParams));

			// Payment
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
					sender, escrowAddress, 1000, null, txParams));

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		public static TransactionGroup PrepareLotteryClaimRewardTransactions(
			ulong yieldlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = Contract.EscrowLogicsigSignature;
			var escrowAddress = escrowSignature.Address;

			var transactions = new List<Transaction>();

			// Call Proxy App w/ arg check
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.ProxyAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx1);

			// Call No Loss Lottery App w/ arg CA
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.LotteryAppId, txParams);

			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("CA"));
			callTx2.accounts.Add(escrowAddress);

			transactions.Add(callTx2);

			// Claim Yieldly from Yieldly Staking
			transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
				escrowAddress,
				sender,
				(long)Constant.YieldlyAssetId,
				yieldlyAmount,
				txParams));

			// Payment
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
					sender, escrowAddress, 1000, null, txParams));

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		#endregion

		#region Yieldly Staking

		public static TransactionGroup PrepareYieldlyStakingDepositTransactions(
			ulong yieldlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = Contract.EscrowLogicsigSignature;
			var escrowAddress = escrowSignature.Address;

			var transactions = new List<Transaction>();

			// Call Proxy App w/ arg check
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.ProxyAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx1);

			// Call Staking App w/ arg S
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.StakingAppId, txParams);

			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("S"));

			transactions.Add(callTx2);

			// Send Yieldly to Escrow address
			transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
				sender,
				escrowAddress,
				(long)Constant.YieldlyAssetId,
				yieldlyAmount,
				txParams));

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		public static TransactionGroup PrepareYieldlyStakingWithdrawTransactions(
			ulong yieldlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = Contract.EscrowLogicsigSignature;
			var escrowAddress = escrowSignature.Address;
			var transactions = new List<Transaction>();

			// Call Proxy App w/ arg check
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.ProxyAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx1);

			// Call Staking App w/ arg W
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.StakingAppId, txParams);

			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("W"));
			callTx2.accounts.Add(escrowAddress);

			transactions.Add(callTx2);

			// Withdraw Yieldly from Escrow address
			transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
				escrowAddress,
				sender,
				(long)Constant.YieldlyAssetId,
				yieldlyAmount,
				txParams));

			// Payment
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
					sender, escrowAddress, 1000, null, txParams));

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		public static TransactionGroup PrepareYieldlyStakingClaimRewardTransactions(
			ulong algoAmount,
			ulong yieldlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = Contract.EscrowLogicsigSignature;
			var escrowAddress = escrowSignature.Address;

			var transactions = new List<Transaction>();

			// Call Proxy App w/ arg check
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.ProxyAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx1);

			// Call Staking App w/ arg CA
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.StakingAppId, txParams);

			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("CA"));
			callTx2.accounts.Add(escrowAddress);

			transactions.Add(callTx2);

			// Call Staking App w/ arg CAL
			var callTx3 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.StakingAppId, txParams);

			callTx3.onCompletion = OnCompletion.Noop;
			callTx3.applicationArgs = new List<byte[]>();
			callTx3.applicationArgs.Add(Strings.ToUtf8ByteArray("CAL"));
			callTx3.accounts.Add(escrowAddress);

			transactions.Add(callTx3);

			// Claim Yieldly from Yieldly Staking
			transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
				escrowAddress,
				sender,
				(long)Constant.YieldlyAssetId,
				yieldlyAmount,
				txParams));

			// Claim Algo from Yieldly Staking
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
				escrowAddress, sender, algoAmount, null, txParams));

			// Payment
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
					sender, escrowAddress, 2000, null, txParams));

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		#endregion

		#region ASA Staking

		public static TransactionGroup PrepareAsaStakingPoolDepositTransactions(
			ulong stakeAmount,
			AsaStakingPool pool,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = pool.LogicsigSignature;
			var escrowAddress = escrowSignature.Address;

			var transactions = new List<Transaction>();

			// Call Staking App w/ arg check
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, pool.ApplicationId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx1);

			// Call Staking App w/ arg S
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, pool.ApplicationId, txParams);

			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("S"));

			transactions.Add(callTx2);

			// Send amount to Escrow address
			transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
				sender,
				escrowAddress,
				pool.StakeAsset.Id,
				stakeAmount,
				txParams));

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		public static TransactionGroup PrepareAsaStakingPoolDepositTransactionsTeal5(
			ulong stakeAmount,
			AsaStakingPoolTeal5 pool,
			Address sender,
			TransactionParametersResponse txParams,
			int random = 0) {

			var escrowAddress = new Address(pool.Address);
			var transactions = new List<Transaction>();

			if (random < 0 || random > 10000) {
				random = (new Random()).Next(1, 10000);
			}

			// Call Staking App w/ arg 'stake'
			transactions.Add(TxnFactory.AppCall(
				sender,
				pool.ApplicationId,
				txParams,
				applicationArgs: new[] {
					ApplicationArgument.String("stake")
				}));

			// Send amount to Escrow address
			transactions.Add(TxnFactory.Pay(
				sender,
				escrowAddress,
				stakeAmount,
				pool.StakeAsset.Id,
				txParams));

			// Call Staking App w/ arg 'bail'
			transactions.Add(TxnFactory.AppCall(
			sender,
			pool.ApplicationId,
			txParams,
			applicationArgs: new[] {
				ApplicationArgument.String("bail"),
				ApplicationArgument.Number((ulong)random)
			}));

			return new TransactionGroup(transactions);
		}

		public static TransactionGroup PrepareAsaStakingPoolWithdrawTransactions(
			ulong withdrawAmount,
			AsaStakingPool pool,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = pool.LogicsigSignature;
			var escrowAddress = escrowSignature.Address;
			var transactions = new List<Transaction>();

			// Call Staking App w/ arg check
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, pool.ApplicationId, txParams);

			callTx1.fee = 1000;
			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx1);

			// Call Staking App w/ arg W
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, pool.ApplicationId, txParams);

			callTx2.fee = 2000;
			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("W"));
			callTx2.accounts.Add(escrowAddress);

			transactions.Add(callTx2);

			// Withdraw amount from Escrow address
			var xferTx = Algorand.Utils.GetTransferAssetTransaction(
				escrowAddress,
				sender,
				pool.StakeAsset.Id,
				withdrawAmount,
				txParams);

			// Passing flatFee: 0 to the utility method is ignored
			xferTx.fee = 0;

			transactions.Add(xferTx);

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		public static TransactionGroup PrepareAsaStakingPoolWithdrawTransactionsTeal5(
			ulong withdrawAmount,
			AsaStakingPoolTeal5 pool,
			Address sender,
			TransactionParametersResponse txParams,
			int random = 0) {

			var transactions = new List<Transaction>();

			if (random < 0 || random > 10000) {
				random = (new Random()).Next(1, 10000);
			}

			// Call Staking App w/ arg 'withdraw'
			transactions.Add(TxnFactory.AppCall(
				sender,
				pool.ApplicationId,
				txParams,
				fee: 2000,
				foreignAssets: new[] {
					pool.StakeAsset.Id
				},
				applicationArgs: new[] {
					ApplicationArgument.String("withdraw"),
					ApplicationArgument.Number(withdrawAmount)
				}));

			// Call Staking App w/ arg 'bail'
			transactions.Add(TxnFactory.AppCall(
			sender,
			pool.ApplicationId,
			txParams,
			applicationArgs: new[] {
				ApplicationArgument.String("bail"),
				ApplicationArgument.Number((ulong)random)
			}));

			return new TransactionGroup(transactions);
		}

		public static TransactionGroup PrepareAsaStakingPoolClaimRewardTransactions(
			ulong rewardAmount,
			AsaStakingPool pool,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = pool.LogicsigSignature;
			var escrowAddress = escrowSignature.Address;

			var transactions = new List<Transaction>();

			// Call Staking App w/ arg check
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, pool.ApplicationId, txParams);

			callTx1.fee = 1000;
			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx1);

			// Call Staking App w/ arg CA
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, pool.ApplicationId, txParams);

			callTx2.fee = 2000;
			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("CA"));
			callTx2.accounts.Add(escrowAddress);

			transactions.Add(callTx2);

			// Claim amount staking reward from escrow address
			var xferTx = Algorand.Utils.GetTransferAssetTransaction(
				escrowAddress,
				sender,
				pool.RewardAsset.Id,
				rewardAmount,
				txParams);

			// Passing flatFee: 0 to the utility method is ignored
			xferTx.fee = 0;

			transactions.Add(xferTx);

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		public static TransactionGroup PrepareAsaStakingPoolClaimRewardTransactionsTeal5(
			AsaStakingPoolTeal5 pool,
			Address sender,
			TransactionParametersResponse txParams,
			int random = 0) {

			var transactions = new List<Transaction>();

			if (random < 0 || random > 10000) {
				random = (new Random()).Next(1, 10000);
			}

			// Call Staking App w/ arg 'claim'
			transactions.Add(TxnFactory.AppCall(
				sender,
				pool.ApplicationId,
				txParams,
				fee: 2000,
				foreignAssets: new[] {
					pool.RewardAsset.Id
				},
				applicationArgs: new[] {
					ApplicationArgument.String("claim"),
				}));

			// Call Staking App w/ arg 'bail'
			transactions.Add(TxnFactory.AppCall(
			sender,
			pool.ApplicationId,
			txParams,
			applicationArgs: new[] {
				ApplicationArgument.String("bail"),
				ApplicationArgument.Number((ulong)random)
			}));

			return new TransactionGroup(transactions);
		}

		#endregion

		#region Utility Transactions

		public static TransactionGroup PrepareAssetOptInTransactions(
			ulong assetId,
			Address sender,
			TransactionParametersResponse txParams) {

			var transactions = new List<Transaction>() {
				Algorand.Utils.GetAssetOptingInTransaction(sender, assetId, txParams)
			};	

			return new TransactionGroup(transactions);
		}

		public static TransactionGroup PrepareAssetOptOutTransactions(
			ulong assetId,
			Address closeTo,
			Address sender,
			TransactionParametersResponse txParams) {

			if (closeTo == null) {
				throw new ArgumentException(
					$"'{nameof(closeTo)}' cannot be null.");
			}

			var transactions = new List<Transaction>(){
				Algorand.Utils.GetTransferAssetTransaction(
					sender, sender, assetId, 0, txParams, closeTo: closeTo)
			};			

			return new TransactionGroup(transactions);
		}

		#endregion

	}

}

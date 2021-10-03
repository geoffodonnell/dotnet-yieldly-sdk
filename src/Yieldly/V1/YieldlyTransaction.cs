using Algorand;
using Algorand.V2.Model;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Yieldly.V1.Model;
using Transaction = Algorand.Transaction;

namespace Yieldly.V1 {

	public static class YieldlyTransaction {

		#region Opt In/Out
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

		public static TransactionGroup PrepareOptOutTransactions(
			Address sender,
			Address closeTo,
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
			Address sender,
			YieldlyAsaStakingPool pool,
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
				(long)pool.StakeAsset.Id,
				stakeAmount,
				txParams));

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		public static TransactionGroup PrepareAsaStakingPoolWithdrawTransactions(
			ulong stakeAmount,
			Address sender,
			YieldlyAsaStakingPool pool,
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
				(long)pool.StakeAsset.Id,
				stakeAmount,
				txParams);

			// Passing flatFee: 0 to the utility method is ignored
			xferTx.fee = 0;

			transactions.Add(xferTx);

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		public static TransactionGroup PrepareAsaStakingPoolClaimRewardTransactions(
			ulong rewardAmount,
			Address sender,
			YieldlyAsaStakingPool pool,
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
				(long)pool.RewardAsset.Id,
				rewardAmount,
				txParams);

			// Passing flatFee: 0 to the utility method is ignored
			xferTx.fee = 0;

			transactions.Add(xferTx);

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}
		#endregion

	}

}

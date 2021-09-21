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

		public static TransactionGroup PrepareNoLossLotteryDepositTransactions(			
			ulong algoAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			// TODO

			return null;
		}

		public static TransactionGroup PrepareYieldyStakingDepositTransactions(
			ulong yieldlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			// TODO

			return null;
		}

		public static TransactionGroup PrepareNoLossLotteryWithdrawTransactions(
			ulong algoAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			// TODO

			return null;
		}

		public static TransactionGroup PrepareYieldyStakingWithdrawTransactions(
			ulong yieldlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			// TODO

			return null;
		}

		public static TransactionGroup PrepareNoLossLottertyClaimRewardTransactions(
			ulong yeildlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = Contract.EscrowLogicsigSignature;
			var escrowAddress = escrowSignature.Address;

			var transactions = new List<Transaction>();

			// Payment
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
					sender, escrowAddress, 1000, null, txParams));

			// Claim Yieldly from Yieldly Staking
			transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
				escrowAddress,
				sender,
				(long)Constant.YieldlyAssetId,
				yeildlyAmount,
				txParams));

			// Call No Loss Lottery App w/ arg CA
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.NoLossLotteryAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("CA"));
			callTx1.accounts.Add(escrowAddress);

			transactions.Add(callTx1);

			// Call Proxy App w/ arg check
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.ProxyAppId, txParams);

			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx2);

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

		public static TransactionGroup PrepareYieldlyStakingClaimRewardTransactions(
			ulong algoAmount,
			ulong yeildlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = Contract.EscrowLogicsigSignature;
			var escrowAddress = escrowSignature.Address;

			var transactions = new List<Transaction>();

			// Payment
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
					sender, escrowAddress, 2000, null, txParams));

			// Claim Algo from Yieldly Staking
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
				escrowAddress, sender, algoAmount, null, txParams));

			// Claim Yieldly from Yieldly Staking
			transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
				escrowAddress,
				sender,
				(long)Constant.YieldlyAssetId,
				yeildlyAmount,
				txParams));

			// Call Staking App w/ arg CAL
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.StakingAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("CAL"));
			callTx1.accounts.Add(escrowAddress);

			transactions.Add(callTx1);

			// Call Staking App w/ arg CA
			var callTx2 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.StakingAppId, txParams);

			callTx2.onCompletion = OnCompletion.Noop;
			callTx2.applicationArgs = new List<byte[]>();
			callTx2.applicationArgs.Add(Strings.ToUtf8ByteArray("CA"));
			callTx2.accounts.Add(escrowAddress);

			transactions.Add(callTx2);

			// Call Proxy App w/ arg check
			var callTx3 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.ProxyAppId, txParams);

			callTx3.onCompletion = OnCompletion.Noop;
			callTx3.applicationArgs = new List<byte[]>();
			callTx3.applicationArgs.Add(Strings.ToUtf8ByteArray("check"));

			transactions.Add(callTx3);

			var result = new TransactionGroup(transactions);

			result.SignWithLogicSig(escrowSignature);

			return result;
		}

	}

}

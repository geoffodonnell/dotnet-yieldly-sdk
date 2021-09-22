using Algorand;
using Algorand.V2.Model;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Yieldly.V1.Model;
using Transaction = Algorand.Transaction;

namespace Yieldly.V1 {

	// Doesn't include transactions for claiming lottery winnings
	// This tx group looks like a claim: https://algoexplorer.io/tx/group/dEKgy0JxE%2FzwAd2ySLkU2nucXC2ALlxLlhoGYzrXl5o%3D
	// Would need to sort out the calculation for winning amount

	public static class YieldlyTransaction {

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

		#region No Loss Lottery Transactions
		public static TransactionGroup PrepareLotteryDepositTransactions(			
			ulong algoAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = Contract.EscrowLogicsigSignature;
			var escrowAddress = escrowSignature.Address;

			var transactions = new List<Transaction>();

			// Payment
			transactions.Add(Algorand.Utils.GetPaymentTransaction(
					sender, escrowAddress, algoAmount, null, txParams));

			// Call No Loss Lottery App w/ arg D
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.LotteryAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("D"));

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

		public static TransactionGroup PrepareLotteryWithdrawTransactions(
			ulong algoAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			// TODO

			return null;
		}

		public static TransactionGroup PrepareNoLossLottertyClaimRewardTransactions(
			ulong algoAmount,
			ulong yieldlyAmount,
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
				yieldlyAmount,
				txParams));

			// Call No Loss Lottery App w/ arg CA
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.LotteryAppId, txParams);

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
		#endregion

		#region Yieldly Staking
		public static TransactionGroup PrepareYieldlyStakingDepositTransactions(
			ulong yieldlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			var escrowSignature = Contract.EscrowLogicsigSignature;
			var escrowAddress = escrowSignature.Address;

			var transactions = new List<Transaction>();

			// Send Yieldly to Escrow address
			transactions.Add(Algorand.Utils.GetTransferAssetTransaction(
				sender,
				escrowAddress,
				(long)Constant.YieldlyAssetId,
				yieldlyAmount,
				txParams));

			// Call Staking App w/ arg S
			var callTx1 = Algorand.Utils.GetApplicationCallTransaction(
				sender, Constant.StakingAppId, txParams);

			callTx1.onCompletion = OnCompletion.Noop;
			callTx1.applicationArgs = new List<byte[]>();
			callTx1.applicationArgs.Add(Strings.ToUtf8ByteArray("S"));

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

		public static TransactionGroup PrepareYieldlyStakingWithdrawTransactions(
			ulong yieldlyAmount,
			Address sender,
			TransactionParametersResponse txParams) {

			// TODO

			return null;
		}

		public static TransactionGroup PrepareYieldlyStakingClaimRewardTransactions(
			ulong algoAmount,
			ulong yieldlyAmount,
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
				yieldlyAmount,
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
		#endregion

	}

}

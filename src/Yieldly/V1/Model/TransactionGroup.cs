using Algorand;
using Algorand.V2;
using Algorand.V2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Yieldly.Patch;
using Account = Algorand.Account;
using LogicsigSignature = Algorand.LogicsigSignature;
using SignedTransaction = Algorand.SignedTransaction;
using Transaction = Algorand.Transaction;

namespace Yieldly.V1.Model {

	public class TransactionGroup {

		public virtual Transaction[] Transactions { get; }

		public virtual SignedTransaction[] SignedTransactions { get; }

		public virtual bool IsSigned => SignedTransactions.All(s => s != null);

		public TransactionGroup(IEnumerable<Transaction> transactions)
			: this(transactions, true) { }

		public TransactionGroup(IEnumerable<Transaction> transactions, bool usePatch) {

			Transactions = transactions.Select(s => usePatch ? PatchTransaction.Create(s) : s).ToArray();
			SignedTransactions = new SignedTransaction[Transactions.Length];

			var gid = TxGroup.ComputeGroupID(Transactions);

			foreach (var tx in Transactions) {
				tx.AssignGroupID(gid);
			}
		}

		public virtual void Sign(Account account) {

			PerformSign(account.Address, s => account.SignTransaction(s));
		}

		public virtual void SignWithLogicSig(LogicsigSignature logicsig) {

			PerformSign(logicsig.Address, s => SignLogicsigTransaction(logicsig, s));
		}

		public virtual void SignWithPrivateKey(byte[] privateKey) {

			var account = Account.AccountFromPrivateKey(privateKey);

			PerformSign(account.Address, s => account.SignTransaction(s));
		}

		internal PostTransactionsResponse Submit(AlgodApi algodApi, bool wait = false) {
			
			if (!IsSigned) {
				throw new Exception(
					"Transaction group has not been signed.");
			}

			var bytes = new List<byte>();

			foreach (var tx in SignedTransactions) {
				 bytes.AddRange(Algorand.Encoder.EncodeToMsgPack(tx));
			}
			
			var response = algodApi.RawTransactionWithHttpInfo(bytes.ToArray());

			if (wait) {
				Algorand.Utils.WaitTransactionToComplete(algodApi, response.Data.TxId);
			}

			return response.Data;
		}

		protected virtual void PerformSign(
			Address sender, Func<Transaction, SignedTransaction> action) {

			if (Transactions == null || Transactions.Length == 0) {
				return;
			}
			
			for (var i = 0; i < Transactions.Length; i++) {
				if (Transactions[i].sender.Equals(sender)) {
					var signed = action(Transactions[i]);
					SignedTransactions[i] = signed;
				}
			}
		}

		private static SignedTransaction SignLogicsigTransaction(
			LogicsigSignature logicsig, Transaction tx) {

			try {
				return Account.SignLogicsigTransaction(logicsig, tx);
			} catch (Exception ex) {
				if (tx.sender.Equals(logicsig.Address)) {
					return new SignedTransaction(tx, logicsig, tx.TxID());
				}

				throw;
			}			
		}

	}

}

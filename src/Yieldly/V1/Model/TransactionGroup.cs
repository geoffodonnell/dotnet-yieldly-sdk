using Algorand;
using Algorand.V2.Algod;
using Algorand.V2.Algod.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Account = Algorand.Account;
using LogicsigSignature = Algorand.LogicsigSignature;
using SignedTransaction = Algorand.SignedTransaction;
using Transaction = Algorand.Transaction;

namespace Yieldly.V1.Model {

	public class TransactionGroup {

		/// <summary>
		/// The transactions in the group
		/// </summary>
		public virtual Transaction[] Transactions { get; }

		/// <summary>
		/// 
		/// </summary>
		public virtual SignedTransaction[] SignedTransactions { get; }

		/// <summary>
		/// Whether or not all the transactions in the group have been signed
		/// </summary>
		public virtual bool IsSigned => SignedTransactions.All(s => s != null);

		/// <summary>
		/// Create a new <see cref="TransactionGroup"/> 
		/// </summary>
		/// <param name="transactions">The transactions in the group</param>
		public TransactionGroup(IEnumerable<Transaction> transactions) {

			Transactions = transactions.ToArray();
			SignedTransactions = new SignedTransaction[Transactions.Length];

			var gid = TxGroup.ComputeGroupID(Transactions);

			foreach (var tx in Transactions) {
				tx.AssignGroupID(gid);
			}
		}

		/// <summary>
		/// Create a new <see cref="TransactionGroup"/> 
		/// </summary>
		/// <param name="transactions">The transactions in the group</param>
		/// <param name="usePatch">Whether or not to use the patch -- ignored.</param>
		/// <remarks>
		/// This constructor is deprecated and will be removed in a future release.
		/// </remarks>
		[Obsolete]
		public TransactionGroup(IEnumerable<Transaction> transactions, bool usePatch)
			: this(transactions) { }

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

		internal async Task<PostTransactionsResponse> SubmitAsync(DefaultApi client, bool wait = true) {

			if (!IsSigned) {
				throw new Exception(
					"Transaction group has not been signed.");
			}

			var bytes = new List<byte>();

			foreach (var tx in SignedTransactions) {
				bytes.AddRange(Algorand.Encoder.EncodeToMsgPack(tx));
			}

			PostTransactionsResponse response = null;

			using (var payload = new MemoryStream(bytes.ToArray())) {
				response = await client.TransactionsAsync(payload);
			}

			if (wait) {
				await Algorand.Utils.WaitTransactionToComplete(client, response.TxId);
			}

			return response;
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

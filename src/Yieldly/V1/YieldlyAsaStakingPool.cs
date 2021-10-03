using Algorand;
using Algorand.V2;
using Algorand.V2.Model;
using System.Linq;
using Yieldly.V1.Model;
using Account = Algorand.Account;

namespace Yieldly.V1 {

	public class YieldlyAsaStakingPool {

		public ulong ApplicationId { get; set; }

		public SimpleAsset StakeAsset { get; set; }

		public SimpleAsset RewardAsset { get; set; }

		public string Address { get; set; }

		internal YieldlyClient Client { get; set; }

		internal LogicsigSignature LogicsigSignature { get; set; }

		internal Application Application { get; set; }

		//internal AlgodApi AlgodApi { get; set; }

		public void Refresh() {

			Application = Client.AlgodApi.GetApplicationByID((long)ApplicationId);
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

		public virtual TransactionGroup PrepareDepositTransactions(
			Address sender,
			ulong stakeAmount) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolDepositTransactions(stakeAmount, sender, this, txParams);

			return result;
		}

		public virtual TransactionGroup PrepareWithdrawTransactions(
			Address sender,
			ulong stakeAmount) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolWithdrawTransactions(stakeAmount, sender, this, txParams);

			return result;
		}

		public virtual TransactionGroup PrepareClaimRewardTransactions(
			Address sender,
			ulong rewardAmount) {

			var txParams = Client.AlgodApi.TransactionParams();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolClaimRewardTransactions(
					rewardAmount, sender, this, txParams);

			return result;
		}

	}

}

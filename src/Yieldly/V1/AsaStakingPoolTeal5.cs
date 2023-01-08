using Algorand;
using Algorand.Common;
using Algorand.Algod.Model;
using System.Numerics;
using System.Threading.Tasks;
using Yieldly.V1.Model;

namespace Yieldly.V1 {

	public class AsaStakingPoolTeal5 : AsaStakingPool {

		/// <inheritdoc />
		public override AsaStakingPoolType Type { get => AsaStakingPoolType.Teal5; }

		/// <summary>
		/// Calculation precision
		/// </summary>
		public virtual BigInteger Precision { get; set; }

		/// <inheritdoc />
		public override async Task<FetchAsaStakingAmountsResult> FetchAmountsAsync(
			Address address, bool refreshPoolInfo = false) {

			var info = await Client.DefaultApi
				.AccountInformationAsync(address.EncodeAsString(), null, Format.Json);

			if (refreshPoolInfo) {
				await RefreshAsync();
			}

			var status = await Client.DefaultApi.GetStatusAsync();
			var blocks = await Client.DefaultApi.GetBlockAsync(status.LastRound, Format.Json);

			var time = YieldlyUtils.GetBlockTime(blocks);
			var userStake = YieldlyUtils.GetBigInteger(info.AppsLocalState, ApplicationId, "User_Stake");
			var reward = YieldlyEquation.CalculateAsaStakePoolClaimableAmountTeal5(info, Application, time.Value);

			return new FetchAsaStakingAmountsResult {
				Reward = reward?.Asa ?? 0ul,
				Staked = (ulong)userStake
			};
		}

		/// <inheritdoc />
		public override async Task<TransactionGroup> PrepareDepositTransactionsAsync(
			Address sender,
			ulong stakeAmount) {

			var txParams = await Client.DefaultApi.TransactionParamsAsync();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolDepositTransactionsTeal5(stakeAmount, this, sender, txParams);

			return result;
		}

		/// <inheritdoc />
		public override async Task<TransactionGroup> PrepareWithdrawTransactionsAsync(
			Address sender,
			ulong withdrawAmount) {

			var txParams = await Client.DefaultApi.TransactionParamsAsync();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolWithdrawTransactionsTeal5(withdrawAmount, this, sender, txParams);

			return result;
		}

		/// <summary>
		/// Create a transaction group to claim rewards from the stake pool
		/// </summary>
		/// <param name="sender">Address of account</param>
		/// <param name="rewardAmount">Amount to claim</param>
		/// <returns>Transaction group</returns>
		/// <remarks>
		/// Note that 'rewardAmount' is ignored for TEAL5 staking pools, the entire amount
		/// is claimed.
		/// </remarks>
		public override async Task<TransactionGroup> PrepareClaimRewardTransactionsAsync(
			Address sender,
			ulong rewardAmount = 0) {

			var txParams = await Client.DefaultApi.TransactionParamsAsync();

			var result = YieldlyTransaction
				.PrepareAsaStakingPoolClaimRewardTransactionsTeal5(
					this, sender, txParams);

			return result;
		}

	}

}

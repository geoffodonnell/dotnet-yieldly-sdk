using Algorand;
using Algorand.V2;
using Algorand.V2.Model;
using System.Linq;
using Yieldly.V1.Model;

namespace Yieldly.V1 {

	public class YieldlyAsaStakingPool {

		public ulong ApplicationId { get; set; }

		public SimpleAsset StakeAsset { get; set; }

		public SimpleAsset RewardAsset { get; set; }

		public string Address { get; set; }

		internal LogicsigSignature LogicsigSignature { get; set; }

		internal Application Application { get; set; }

		internal AlgodApi AlgodApi { get; set; }

		public FetchAsaStakingPoolAmountsResult FetchAmounts(Address address, bool refreshPoolInfo = false) {

			var accountInfo = AlgodApi.AccountInformation(address.EncodeAsString());

			if (refreshPoolInfo) {
				Application = AlgodApi.GetApplicationByID((long)ApplicationId);
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
	}

}

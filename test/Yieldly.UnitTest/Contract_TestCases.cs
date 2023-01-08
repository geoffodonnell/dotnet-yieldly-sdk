using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using Yieldly.V1;

namespace Yieldly.UnitTest {

	[TestClass]
	public class Contract_TestCases {

		public static StringComparison mCmp = StringComparison.InvariantCulture;

		[TestMethod]
		public void EscrowLogicsigSignature_NotNull() {

			Assert.IsNotNull(Contract.EscrowLogicsigSignature.Address);
		}

		[TestMethod]
		public void GetAsaStakePoolLogicsigSignature_VerifyOpul() {

			var appId = Constant.OpulousStakingAppId;
			var lsig = Contract.GetAsaStakePoolLogicsigSignature(appId);
			var logicAsBase64 = Strings.FromUtf8ByteArray(Base64.Encode(lsig.Logic));

			Assert.IsTrue(String.Equals(
				lsig.Address.EncodeAsString(), "VUY44SYOFFJE3ZIDEMA6PT34J3FAZUAE6VVTOTUJ5LZ343V6WZ3ZJQTCD4", mCmp));

			Assert.IsTrue(String.Equals(
				logicAsBase64, "BCADAgYAMgQiDzIEIw4QQQA5MwAYgZWN/aUBEkAAA0IAKTMAECMSMwAZJBKBBTMAGRIRIjMAGRIREDMBIDIDEhAzACAyAxIQQAACJEOBAUM=", mCmp));
		}

		[TestMethod]
		public void GetAsaStakePoolLogicsigSignature_VerifySmile() {

			var appId = Constant.SmileCoinStakingAppId;
			var lsig = Contract.GetAsaStakePoolLogicsigSignature(appId);
			var logicAsBase64 = Strings.FromUtf8ByteArray(Base64.Encode(lsig.Logic));

			Assert.IsTrue(String.Equals(
				lsig.Address.EncodeAsString(), "KDZS6OV5PAARFJPRZYRRQWCZOCPICB6NJ4YNHZKNCNKIVOLSL5ZCPMY24I", mCmp));

			Assert.IsTrue(String.Equals(
				logicAsBase64, "BCADAgYAMgQiDzIEIw4QQQA5MwAYgdPA86cBEkAAA0IAKTMAECMSMwAZJBKBBTMAGRIRIjMAGRIREDMBIDIDEhAzACAyAxIQQAACJEOBAUM=", mCmp));
		}

	}

}

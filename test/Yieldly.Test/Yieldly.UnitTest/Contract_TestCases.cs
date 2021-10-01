using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yieldly.V1;

namespace Yieldly.UnitTest {

	[TestClass]
	public class Contract_TestCases {

		[TestMethod]
		public void YieldlyToMicroyieldly_WholeNumber() {

			Assert.IsNotNull(Contract.EscrowLogicsigSignature.Address);
		}

	}

}

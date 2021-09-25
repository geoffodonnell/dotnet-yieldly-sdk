using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yieldly.V1;

namespace Yieldly.UnitTest {

	[TestClass]
	public class Util_TestCases {

		[TestMethod]
		public void YieldlyToMicroyieldly_WholeNumber() {

			Assert.IsTrue(YieldlyUtils.YieldlyToMicroyieldly(20d) == 20000000);
		}

		[TestMethod]
		public void YieldlyToMicroyieldly_WithDecimal() {

			Assert.IsTrue(YieldlyUtils.YieldlyToMicroyieldly(20.5) == 20500000);
		}

		[TestMethod]
		public void YieldlyToMicroyieldly_SixDigitsAfterDecimal() {

			Assert.IsTrue(YieldlyUtils.YieldlyToMicroyieldly(20.123456) == 20123456);
		}

		[TestMethod]
		public void YieldlyToMicroyieldly_TruncateSeventhDigitAfterDecimal() {

			Assert.IsTrue(YieldlyUtils.YieldlyToMicroyieldly(20.1234569) == 20123456);
		}

	}

}

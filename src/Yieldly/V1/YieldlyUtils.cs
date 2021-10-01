using System;

namespace Yieldly.V1 {
	
	public static class YieldlyUtils {

		public static ulong YieldlyToMicroyieldly(double yieldly) {
			return Convert.ToUInt64(Math.Floor(yieldly * 1000000));
		}

	}

}

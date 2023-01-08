using Algorand.Common;
using Algorand.Common.Asc;
using Algorand.Algod.Model;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Yieldly.V1 {
	
	public static class YieldlyUtils {

		public static ulong YieldlyToMicroyieldly(double yieldly) {
			return Convert.ToUInt64(Math.Floor(yieldly * 1000000));
		}

        public static BigInteger GetBigInteger(
            ICollection<ApplicationLocalState> state,
            ulong applicationId,
            string key) {

            var base64 = ApplicationState.GetBytes(state, applicationId, key);
            var bytes = Base64.Decode(base64);

            return ToNumber(bytes);
        }

        public static BigInteger GetBigInteger(
            ICollection<TealKeyValue> state,
            string key) {

            var base64 = ApplicationState.GetBytes(state, key);
            var bytes = Base64.Decode(base64);

            return ToNumber(bytes);
        }

        private static BigInteger ToNumber(byte[] value) {

            if (value.Length == 0) {
                return 0;
            }

            var bytes = Pad(value);

#if NET6_0
            return new BigInteger(bytes, true, true);
#elif NETSTANDARD2_0
            var littleEndianBytes = bytes
                .Select(BinaryPrimitives.ReverseEndianness)
                .ToArray();

            return new BigInteger(littleEndianBytes);
#endif
        }

        private static byte[] Pad(byte[] bytes) {

            var parts = (bytes.Length + 7) / 8;
            var result = new byte[parts * 8];

#if NET6_0
            Array.Fill<byte>(result, 0);
#elif NETSTANDARD2_0
            for (var i = 0; i < result.Length; i++) {
                result[i] = 0;
            }
#endif
            Array.Copy(bytes, 0, result, result.Length - bytes.Length, bytes.Length);

            return result;
        }

        public static ulong? GetBlockTime(CertifiedBlock response) {

            return response.Block.Timestamp;
        }

        public static string Base64Decode(string base64String) {

			return Strings.FromUtf8ByteArray(Base64.Decode(base64String));
		}

        internal static byte[] GetProgram(
            ProgramLogic logic, Dictionary<string, object> variables) {

            var template = logic.ByteCode;
            var templateBytes = Base64.Decode(template).ToList();

            if (variables == null) {
                return templateBytes.ToArray();
            }

            var offset = 0;

            foreach (var variable in logic.Variables.OrderBy(s => s.Index)) {

                var name = variable.Name.ToLower();
                var value = variables[name];
                var start = variable.Index - offset;
                var end = start + variable.Length;
                var valueEncoded = EncodeValue(value, variable.Type);
                var valueEncodedLength = valueEncoded.Length;
                var diff = variable.Length - valueEncodedLength;
                offset += diff;

                templateBytes.RemoveRange(start, variable.Length);
                templateBytes.InsertRange(start, valueEncoded);
            }

            return templateBytes.ToArray();
        }

        internal static byte[] EncodeValue(object value, string type) {

            if (String.Equals(type, "int", StringComparison.OrdinalIgnoreCase)) {
                return EncodeInt(value);
            }

            throw new ArgumentException($"Unsupported value type {type}");
        }

        internal static byte[] EncodeInt(object value) {

            var result = new List<byte>();
            var number = Convert.ToUInt64(value);

            while (true) {
                var toWrite = number & 0x7F;

                number >>= 7;

                if (number > 0) {
                    result.Add((byte)(toWrite | 0x80));
                } else {
                    result.Add((byte)toWrite);
                    break;
                }
            }

            return result.ToArray();
        }

    }

}

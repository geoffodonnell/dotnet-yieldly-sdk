using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Linq;
using Yieldly.V1.Asc;

namespace Yieldly.V1 {
	
	public static class YieldlyUtils {

		public static ulong YieldlyToMicroyieldly(double yieldly) {
			return Convert.ToUInt64(Math.Floor(yieldly * 1000000));
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

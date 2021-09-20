using System;

namespace Yieldly.Patch {

    /// <summary>
    /// Copied from: https://github.com/RileyGe/dotnet-algorand-sdk/blob/master/dotnet-algorand-sdk/Util/JavaHelper.cs
    /// to support TEAL4 contracts in the Yieldly SDK until the Algorand SDK is updated, at which time this class
    /// will be removed.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    internal class JavaHelper<T> {

        internal static T[] ArrayCopyRange(T[] original, int from, int to) {
            T[] ret = new T[to - from];
            for (int i = from; i < to; i++) {
                ret[i - from] = original[i];
            }
            return ret;
        }

        internal static T[] ArrayCopyOf(T[] source, int length) {
            T[] ret = new T[length];
            for (int i = 0; i < length && i < source.Length; i++) {
                //if (i > source.Length - 1) break;
                ret[i] = source[i];
            }
            //source.CopyTo(ret, 0);
            return ret;
        }

        internal static void SyatemArrayCopy(T[] src, int srcPos, T[] dest, int destPos, int length) {
            if (src.Length < srcPos + length || dest.Length < destPos + length) {
                throw new ArgumentException("length wrong");
            }
            for (int i = 0; i < length; i++) {
                dest[destPos + i] = src[srcPos + i];
            }
        }

        internal static string ArrayToString(T[] array) {
            string ret = "[";
            foreach (var item in array) {
                ret += item.ToString() + ", ";
            }
            return ret.Substring(0, ret.Length - 2) + "]";
        }

        internal static T RequireNotNull(object o, string message) {
            if (o == null)
                throw new NullReferenceException(message);
            return (T)o;
        }

    }

}

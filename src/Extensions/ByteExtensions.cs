using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeoAgi.Tools.FlatFileQuery.Extensions
{
    internal static class ByteExtensions
    {
        /// <summary>
        /// Convers the bytearray into a Hexidecimal Encoded string
        /// </summary>
        /// <param name="bytes"></param>
        /// <see href="https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/14333437#14333437" />
        /// <returns></returns>
        public static string ToHex(this byte[]? bytes)
        {
            if (bytes == null)
                return string.Empty;

            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        #region Internal Helpers
        private static readonly uint[] _lookup32 = CreateLookup32();

        /// <summary>
        /// Builds an internal reference of hex characters favoring allocation to the heap over memory or binary size
        /// </summary>
        /// <returns></returns>
        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("x2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }
        #endregion
    }
}

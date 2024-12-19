using System;
using System.IO;

namespace ArmaBrowser.Data.DefaultImpl
{
    internal static class MgArrayExtensions
    {
        public static int Read7BitEncodedInt(this BinaryReader reader)
        {
            var val = 0;
            var shift = 0;
            byte b;
            do
            {
                b = reader.ReadByte();
                val += (b & 0x7f) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return val;
        }

        /// <summary>
        ///     Returns the index within this string of the first occurrence of the
        ///     specified substring. If it is not a substring, return -1.
        ///     https://en.wikipedia.org/wiki/Boyer%E2%80%93Moore_string_search_algorithm
        /// </summary>
        /// <param name="source">The array to be scanned.</param>
        /// <param name="offset"></param>
        /// <param name="pattern">The target array to search.</param>
        /// <returns>The start index of the pattern.</returns>
        public static long IndexOf(byte[] source, long offset, byte[] pattern)
        {
            if (pattern.LongLength == 0L)
            {
                return 0;
            }

            for (var i = pattern.LongLength - 1 + offset; i < source.LongLength;)
            {
                long j;
                for (j = pattern.LongLength - 1; pattern[j] == source[i]; --i, --j)
                {
                    if (j == 0)
                    {
                        return i;
                    }
                }
                i += pattern.LongLength - j; // For naive method
                //i += Math.Max(offsetTable[pattern.LongLength - 1 - j], charTable[source[i]]);
            }
            return -1;
        }

        /// <summary>
        ///     Returns the index within this string of the first occurrence of the
        ///     specified substring. If it is not a substring, return -1.
        ///     https://en.wikipedia.org/wiki/Boyer%E2%80%93Moore_string_search_algorithm
        /// </summary>
        /// <param name="source">The array to be scanned.</param>
        /// <param name="pattern">The target array to search.</param>
        /// <returns>The start index of the pattern.</returns>
        public static long IndexOf(byte[] source, byte[] pattern)
        {
            if (pattern.LongLength == 0L)
            {
                return 0;
            }
            var charTable = MakeByteTable(pattern);
            var offsetTable = MakeOffsetTable(pattern);
            for (var i = pattern.LongLength - 1; i < source.LongLength;)
            {
                long j;
                for (j = pattern.LongLength - 1; pattern[j] == source[i]; --i, --j)
                {
                    if (j == 0)
                    {
                        return i;
                    }
                }
                //i += pattern.LongLength - j; // For naive method
                i += Math.Max(offsetTable[pattern.LongLength - 1 - j], charTable[source[i]]);
            }
            return -1;
        }

        /**
         * Makes the jump table based on the mismatched character information.
         */

        private static long[] MakeByteTable(byte[] needle)
        {
            const int alphabetSize = 256;
            var table = new long[alphabetSize];
            for (long i = 0; i < table.LongLength; ++i)
            {
                table[i] = needle.LongLength;
            }
            for (long i = 0; i < needle.LongLength - 1; ++i)
            {
                table[needle[i]] = needle.LongLength - 1 - i;
            }
            return table;
        }

        /**
         * Makes the jump table based on the scan offset which mismatch occurs.
         */

        private static long[] MakeOffsetTable(byte[] needle)
        {
            var table = new long[needle.LongLength];
            var lastPrefixPosition = needle.LongLength;
            for (var i = needle.LongLength - 1; i >= 0; --i)
            {
                if (IsPrefix(needle, i + 1L))
                {
                    lastPrefixPosition = i + 1;
                }
                table[needle.LongLength - 1L - i] = lastPrefixPosition - i + needle.LongLength - 1;
            }
            for (long i = 0; i < needle.LongLength - 1; ++i)
            {
                var slen = SuffixLength(needle, i);
                table[slen] = needle.LongLength - 1 - i + slen;
            }
            return table;
        }

        /**
         * Is needle[p:end] a prefix of needle?
         */

        private static bool IsPrefix(byte[] needle, long p)
        {
            for (long i = p, j = 0; i < needle.LongLength; ++i, ++j)
            {
                if (needle[i] != needle[j])
                {
                    return false;
                }
            }
            return true;
        }

        /**
         * Returns the maximum length of the substring ends at p and is a suffix.
         */

        private static long SuffixLength(byte[] needle, long p)
        {
            var len = 0L;
            for (long i = p, j = needle.LongLength - 1;
                i >= 0 && needle[i] == needle[j];
                --i, --j)
            {
                len += 1;
            }
            return len;
        }

        public static bool IsAscii(this byte value)
        {
            return 19 < value && value < 127;
        }
    }
}
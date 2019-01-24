using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace AspNetCoreWebApi.Storage.StringPools
{
    public static class HashStorageExtensions
    {
        private static Hash HashCode(string str)
        {
            const uint b1 = 41;
            const uint b2 = 43;
            const uint b3 = 47;
            const uint b4 = 53;

            uint m1 = 1;
            uint m2 = 1;
            uint m3 = 1;
            uint m4 = 1;

            uint result1 = 0;
            uint result2 = 0;
            uint result3 = 0;
            uint result4 = 0;

            foreach(char ch in str)
            {
                result1 += m1 * ch;
                m1 *= b1;

                result2 += m2 * ch;
                m2 *= b2;

                result3 += m3 * ch;
                m3 *= b3;

                result4 += m4 * ch;
                m4 *= b4;
            }

            return new Hash() { H41 = (ushort)result1, H43 = (ushort)result2, H47 = (ushort)result3, H53 = (ushort)result4 };
        }

        public static void Add(this HashStorage hashStorage, string str, int id)
        {
            hashStorage.Add(HashCode(str), id);
        }

        public static void ReplaceById(this HashStorage hashStorage, string str, int id)
        {
            hashStorage.ReplaceById(id, HashCode(str));
        }

        public static bool ContainsString(this HashStorage hashStorage, string str)
        {
            return hashStorage.ContainsHash(HashCode(str));
        }

        public static bool TryGetByString(this HashStorage hashStorage, string str, out int id)
        {
            return hashStorage.TryGetByHash(HashCode(str), out id);
        }
    }
}
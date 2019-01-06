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
            const int b1 = 41;
            const int b2 = 43;
            const int b3 = 47;

            int m1 = 1;
            int m2 = 1;
            int m3 = 1;

            int result1 = 0;
            int result2 = 0;
            int result3 = 0;

            foreach(char ch in str)
            {
                result1 += m1 * ch;
                m1 *= b1;

                result2 += m2 * ch;
                m2 *= b2;

                result3 += m3 * ch;
                m3 *= b3;
            }

            return new Hash() { H41 = result1, H43 = result2, H47 = result3 };
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
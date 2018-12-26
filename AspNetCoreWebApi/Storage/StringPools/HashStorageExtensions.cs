using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace AspNetCoreWebApi.Storage.StringPools
{
    public static class HashStorageExtensions
    {
        public static void Add(this HashStorage hashStorage, string str, int id)
        {
            hashStorage.Add(str.GetHashCode(), id);
        }

        public static bool ContainsString(this HashStorage hashStorage, string str)
        {
            return hashStorage.ContainsHash(str.GetHashCode());
        }

        public static bool TryGetByString(this HashStorage hashStorage, string str, out int id)
        {
            return hashStorage.TryGetByHash(str.GetHashCode(), out id);
        }
    }
}
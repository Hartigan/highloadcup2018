using System;
using System.Collections.Generic;
using System.Threading;

namespace AspNetCoreWebApi.Storage
{
    public static class HashStorageExtensions
    {
        public static void Add(this HashStorage hashStorage, string str)
        {
            hashStorage.Add(str.GetHashCode());
        }

        public static bool Contains(this HashStorage hashStorage, string str)
        {
            return hashStorage.Contains(str.GetHashCode());
        }
    }

    public class HashStorage
    {
        private HashSet<int> _set = new HashSet<int>();
        private ReaderWriterLock _rw = new ReaderWriterLock();

        public HashStorage()
        {
        }

        public void Add(int item)
        {
            _rw.AcquireWriterLock(2000);
            _set.Add(item);
            _rw.ReleaseWriterLock();
        }

        public bool Contains(int item)
        {
            _rw.AcquireReaderLock(2000);
            var result = _set.Contains(item);
            _rw.ReleaseReaderLock();
            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class BaseContext<T>
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, T> _id2item = new SortedDictionary<int, T>();

        public BaseContext()
        {
        }

        public void Add(int id, T value)
        {
            _rw.AcquireWriterLock(2000);
            _id2item[id] = value;
            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out T value)
        {
            _rw.AcquireReaderLock(2000);
            var result = _id2item.TryGetValue(id, out value);
            _rw.ReleaseReaderLock();
            return result;
        }
    }
}
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AspNetCoreWebApi.Storage
{
    public class IdStorage
    {
        private bool[] _set = new bool[DataConfig.MaxId];
        private ReaderWriterLock _rw = new ReaderWriterLock();

        public IdStorage()
        {
        }

        public void Add(int item)
        {
            _rw.AcquireWriterLock(2000);
            _set[item] = true;
            _rw.ReleaseWriterLock();
        }

        public bool Contains(int item)
        {
            _rw.AcquireReaderLock(2000);
            var result = _set[item];
            _rw.ReleaseReaderLock();
            return result;
        }

        public IEnumerable<int> AsEnumerable()
        {
            for (int i = 0; i < DataConfig.MaxId; i++)
            {
                if (_set[i])
                {
                    yield return i;
                }
            }
        }
    }
}
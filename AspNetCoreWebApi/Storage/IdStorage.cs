using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AspNetCoreWebApi.Storage
{
    public class IdStorage
    {
        private HashSet<int> _set = new HashSet<int>();
        private ReaderWriterLock _rw = new ReaderWriterLock();

        public IdStorage()
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

        public IEnumerable<int> Except(IEnumerable<int> except)
        {
            return _set.AsEnumerable().Except(except);
        }

        public IEnumerable<int> AsEnumerable()
        {
            return _set.AsEnumerable();
        }
    }
}
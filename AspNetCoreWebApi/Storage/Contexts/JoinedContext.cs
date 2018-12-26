using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class JoinedContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, DateTimeOffset> _id2time = new SortedDictionary<int, DateTimeOffset>();

        public JoinedContext()
        {
        }

        public void AddOrUpdate(int id, DateTimeOffset time)
        {
            _rw.AcquireWriterLock(2000);
            _id2time[id] = time;
            _rw.ReleaseWriterLock();
        }

        public IEnumerable<int> Filter(GroupRequest.JoinedRequest joined)
        {
            return _id2time.Where(x => x.Value.Year == joined.Year).Select(x => x.Key);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class BirthContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, DateTimeOffset> _id2time = new SortedDictionary<int, DateTimeOffset>();

        public BirthContext()
        {
        }

        public void AddOrUpdate(int id, DateTimeOffset time)
        {
            _rw.AcquireWriterLock(2000);
            _id2time[id] = time;
            _rw.ReleaseWriterLock();
        }

        public DateTimeOffset Get(int id) => _id2time[id];

        public IEnumerable<int> Filter(FilterRequest.BirthRequest birth)
        {
            return _id2time.Where(x =>
            {
                if (birth.Gt.HasValue && x.Value <= birth.Gt.Value)
                {
                    return false;
                }

                if (birth.Lt.HasValue && x.Value >= birth.Lt.Value)
                {
                    return false;
                }

                if (birth.Year.HasValue && x.Value.Year != birth.Year.Value)
                {
                    return false;
                }

                return true;
            }).Select(x => x.Key);
        }

        public IEnumerable<int> Filter(GroupRequest.BirthRequest birth)
        {
            return _id2time.Where(x => x.Value.Year == birth.Year).Select(x => x.Key);
        }
    }
}
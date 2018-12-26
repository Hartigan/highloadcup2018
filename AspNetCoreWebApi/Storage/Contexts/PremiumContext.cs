using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class PremiumContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, Premium> _id2item = new SortedDictionary<int, Premium>();
        private HashSet<int> _now = new HashSet<int>();


        public PremiumContext()
        {
        }

        public void AddOrUpdate(int id, Premium item)
        {
            _rw.AcquireWriterLock(2000);
            if (_id2item.ContainsKey(id))
            {
                _id2item.Remove(id);
                _now.Remove(id);
            }

            _id2item[id] = item;
            if (item.IsNow())
            {
                _now.Add(id);
            }
            
            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out Premium premium) => _id2item.TryGetValue(id, out premium);

        public IEnumerable<int> Filter(
            FilterRequest.PremiumRequest premium,
            IdStorage ids)
        {
            if (premium.IsNull.HasValue)
            {
                if (premium.IsNull.Value)
                {
                    return premium.Now ? Enumerable.Empty<int>() : ids.Except(_id2item.Keys);
                }
            }

            if (premium.Now)
            {
                return _now;
            }
            else
            {
                return _id2item.Keys;
            }
        }

        public bool IsNow(int x) => _now.Contains(x);
    }
}
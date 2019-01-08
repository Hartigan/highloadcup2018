using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class PremiumContext : IBatchLoader<Premium>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private Premium?[] _premiums = new Premium?[DataConfig.MaxId];
        private HashSet<int> _now = new HashSet<int>();
        private HashSet<int> _ids = new HashSet<int>();


        public PremiumContext()
        {
        }

        public void LoadBatch(IEnumerable<BatchEntry<Premium>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach(var entry in batch)
            {
                _premiums[entry.Id] = entry.Value;
                _ids.Add(entry.Id);
                if (entry.Value.IsNow())
                {
                    _now.Add(entry.Id);
                }
            }

            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, Premium item)
        {
            _rw.AcquireWriterLock(2000);

            var prev = _premiums[id];

            if (prev.HasValue)
            {
                if (prev.Value.IsNow() != item.IsNow())
                {
                    if (prev.Value.IsNow())
                    {
                        _now.Remove(id);
                    }
                    else
                    {
                        _now.Add(id);
                    }
                }
            }
            else
            {
                _ids.Add(id);
                if (item.IsNow())
                {
                    _now.Add(id);
                }
            }

            _premiums[id] = item;

            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out Premium premium)
        {
            if (_premiums[id].HasValue)
            {
                premium = _premiums[id].Value;
                return true;
            }
            else
            {
                premium = default(Premium);
                return false;
            }
        }

        public IEnumerable<int> Filter(
            FilterRequest.PremiumRequest premium,
            IdStorage ids)
        {
            if (premium.IsNull.HasValue)
            {
                if (premium.IsNull.Value)
                {
                    return premium.Now ? Enumerable.Empty<int>() : ids.Except(_ids);
                }
            }

            if (premium.Now)
            {
                return _now;
            }
            else
            {
                return _ids;
            }
        }

        public bool IsNow(int id) => _now.Contains(id);

        public void Compress()
        {
        }
    }
}
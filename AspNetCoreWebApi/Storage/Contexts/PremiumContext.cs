using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class PremiumContext : IBatchLoader<Premium>, ICompresable
    {
        private Premium[] _premiums = new Premium[DataConfig.MaxId];
        
        private DelaySortedList<int> _ids = DelaySortedList<int>.CreateDefault();
        private DelaySortedList<int> _null = DelaySortedList<int>.CreateDefault();
        private DelaySortedList<int> _now = DelaySortedList<int>.CreateDefault();

        public PremiumContext()
        {
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach(var id in ids.AsEnumerable())
            {
                if (!_premiums[id].IsNotEmpty())
                {
                    _null.Load(id);
                }
            }
            _null.LoadEnded();
        }

        public void LoadBatch(int id, Premium premium)
        {
            _premiums[id] = premium;
            _ids.Load(id);
            if (premium.IsNow())
            {
                _now.Load(id);
            }
        }

        public void AddOrUpdate(int id, Premium item)
        {
            var prev = _premiums[id];

            if (_premiums[id].IsNotEmpty())
            {
                if (prev.IsNow() != item.IsNow())
                {
                    if (prev.IsNow())
                    {
                        _now.DelayRemove(id);
                    }
                    else
                    {
                        _now.DelayAdd(id);
                    }
                }
            }
            else
            {
                _ids.DelayAdd(id);
                if (item.IsNow())
                {
                    _now.DelayAdd(id);
                }
            }

            _premiums[id] = item;
        }

        public bool TryGet(int id, out Premium premium)
        {
            if (_premiums[id].IsNotEmpty())
            {
                premium = _premiums[id];
                return true;
            }
            else
            {
                premium = default(Premium);
                return false;
            }
        }

        public IIterator<int> Filter(
            FilterRequest.PremiumRequest premium,
            IdStorage ids)
        {
            if (premium.IsNull.HasValue)
            {
                if (premium.IsNull.Value)
                {
                    return premium.Now ? ListHelper.EmptyInt : _null.GetIterator();
                }
            }

            if (premium.Now)
            {
                return _now.GetIterator();
            }
            else
            {
                return _ids.GetIterator();
            }
        }

        public bool IsNow(int id) => _premiums[id].IsNow();

        public void Compress()
        {
            _ids.Flush();
            _now.Flush();
        }

        public void LoadEnded()
        {
            _ids.LoadEnded();
            _now.LoadEnded();
        }
    }
}
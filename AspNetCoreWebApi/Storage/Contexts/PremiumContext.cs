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
        
        private CountSet _ids = new CountSet();
        private CountSet _null = new CountSet();
        private CountSet _now = new CountSet();

        public PremiumContext()
        {
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach(var id in ids.AsEnumerable())
            {
                if (!_ids.Contains(id))
                {
                    _null.Add(id);
                }
            }
        }

        public void LoadBatch(int id, Premium premium)
        {
            _premiums[id] = premium;
            _ids.Add(id);
            if (premium.IsNow())
            {
                _now.Add(id);
            }
        }

        public void AddOrUpdate(int id, Premium item)
        {
            var prev = _premiums[id];

            if (_ids.Contains(id))
            {
                if (prev.IsNow() != item.IsNow())
                {
                    if (prev.IsNow())
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
        }

        public bool TryGet(int id, out Premium premium)
        {
            if (_null.Contains(id))
            {
                premium = default(Premium);
                return false;
            }
            else
            {
                premium = _premiums[id];
                return true;
            }
        }

        public IFilterSet Filter(
            FilterRequest.PremiumRequest premium,
            IdStorage ids)
        {
            if (premium.IsNull.HasValue)
            {
                if (premium.IsNull.Value)
                {
                    return premium.Now ? (IFilterSet)FilterSet.Empty : _null;
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

        public void LoadEnded()
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class CountryContext : IBatchLoader<short>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private short?[] _raw = new short?[DataConfig.MaxId];
        private Dictionary<short, FilterSet> _id2AccId = new Dictionary<short, FilterSet>();
        private FilterSet _null = new FilterSet();
        private FilterSet _ids = new FilterSet();

        public CountryContext()
        {
        }

        public void Add(int id, short countryId)
        {
            _rw.AcquireWriterLock(2000);
            _raw[id] = countryId;
            _ids.Add(id);
            if (!_id2AccId.ContainsKey(countryId))
            {
                _id2AccId[countryId] = new FilterSet();
            }
            _id2AccId[countryId].Add(id);
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, short countryId)
        {
            _rw.AcquireWriterLock(2000);
            foreach(var value in _id2AccId.Values)
            {
                value.Remove(id);
            }

            Add(id, countryId);

            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out short countryId)
        {
            countryId = 0;
            bool res = false;
            foreach(var pair in _id2AccId)
            {
                if (pair.Value.Contains(id))
                {
                    res = true;
                    countryId = pair.Key;
                    break;
                }
            }
            return res;
        }

        public short? Get(int id)
        {
            return _raw[id];
        }

        public FilterSet Filter(
            FilterRequest.CountryRequest country,
            IdStorage ids,
            CountryStorage countries)
        {
            if (country.IsNull.HasValue)
            {
                if (country.IsNull.Value)
                {
                    return country.Eq == null
                        ? _null
                        : FilterSet.Empty;
                }
            }

            if (country.Eq == null)
            {
                return _ids;
            }
            short countryId = countries.Get(country.Eq);

            if (_id2AccId.ContainsKey(countryId))
            {
                return _id2AccId[countryId];
            }
            else
            {
                return FilterSet.Empty;
            }
        }

        public FilterSet Filter(
            GroupRequest.CountryRequest country,
            CountryStorage countries)
        {
            short countryId = countries.Get(country.Country);

            if (_id2AccId.ContainsKey(countryId))
            {
                return _id2AccId[countryId];
            }
            else
            {
                return FilterSet.Empty;
            }
        }

        public void FillGroups(List<int?> groups)
        {
            groups.AddRange(_id2AccId.Keys.Cast<int?>());
            groups.Add(null);
        }

        public bool Contains(short? countryId, int id)
        {
            return _raw[id] == countryId;
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach(var id in ids.AsEnumerable())
            {
                if (_raw[id] == null)
                {
                    _null.Add(id);
                }
            }
        }

        public void LoadBatch(IEnumerable<BatchEntry<short>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach (var entry in batch)
            {
                _raw[entry.Id] = entry.Value;
                _ids.Add(entry.Id);
                if (!_id2AccId.ContainsKey(entry.Value))
                {
                    _id2AccId[entry.Value] = new FilterSet();
                }
                _id2AccId[entry.Value].Add(entry.Id);
            }

            _rw.ReleaseWriterLock();
        }

        public void Compress()
        {
        }
    }
}
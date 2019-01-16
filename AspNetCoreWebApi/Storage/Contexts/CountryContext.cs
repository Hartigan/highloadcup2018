using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class CountryContext : IBatchLoader<short>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private short?[] _raw = new short?[DataConfig.MaxId];
        private Dictionary<short, List<int>> _id2AccId = new Dictionary<short, List<int>>();
        private HashSet<int> _null = new HashSet<int>();

        public CountryContext()
        {
        }

        public void Add(int id, short countryId)
        {
            _rw.AcquireWriterLock(2000);
            _raw[id] = countryId;
            if (_id2AccId.ContainsKey(countryId))
            {
                var list = _id2AccId[countryId];
                list.Insert(~list.BinarySearch(id), id);
            }
            else
            {
                _id2AccId[countryId] = new List<int>() { id };
            }
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, short countryId)
        {
            _rw.AcquireWriterLock(2000);
            foreach(var value in _id2AccId.Values)
            {
                int index = value.BinarySearch(id);
                if (index >= 0)
                {
                    value.RemoveAt(index);
                    break;
                }
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
                int index = pair.Value.BinarySearch(id);
                if (index >= 0)
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

        public IEnumerable<int> Filter(
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
                        : Enumerable.Empty<int>();
                }
            }

            if (country.Eq == null)
            {
                return _id2AccId.SelectMany(x => x.Value);
            }
            short countryId = countries.Get(country.Eq);

            if (_id2AccId.ContainsKey(countryId))
            {
                return _id2AccId[countryId];
            }
            else
            {
                return Enumerable.Empty<int>();
            }
        }

        public IEnumerable<int> Filter(
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
                return Enumerable.Empty<int>();
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
            _null.UnionWith(ids.AsEnumerable());
            _null.ExceptWith(_id2AccId.Values.SelectMany(x => x));
            _null.TrimExcess();
        }

        public void GetByCountryId(
            short? countryId,
            HashSet<int> currentIds,
            IdStorage ids)
        {
            if (countryId.HasValue)
            {
                currentIds.UnionWith(_id2AccId[countryId.Value]);
            }
            else
            {
                currentIds.UnionWith(_null);
            }
        }

        public void LoadBatch(IEnumerable<BatchEntry<short>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach (var entry in batch)
            {
                _raw[entry.Id] = entry.Value;
                if (_id2AccId.ContainsKey(entry.Value))
                {
                    _id2AccId[entry.Value].Add(entry.Id);
                }
                else
                {
                    _id2AccId[entry.Value] = new List<int>() { entry.Id };
                }
            }

            foreach (var countryId in batch.Select(x => x.Value).Distinct())
            {
                _id2AccId[countryId].Sort();
            }

            _rw.ReleaseWriterLock();
        }

        public void Compress()
        {
            _null.TrimExcess();
            foreach(var list in _id2AccId.Values)
            {
                list.Compress();
            }
        }
    }
}
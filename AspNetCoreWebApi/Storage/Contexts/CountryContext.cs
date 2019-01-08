using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class CountryContext : IBatchLoader<int>
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, List<int>> _id2AccId = new SortedDictionary<int, List<int>>();

        public CountryContext()
        {
        }

        public void Add(int id, int countryId)
        {
            _rw.AcquireWriterLock(2000);
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

        public void AddOrUpdate(int id, int countryId)
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

        public bool TryGet(int id, out int countryId)
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
                        ? ids.Except(_id2AccId.SelectMany(x => x.Value))
                        : Enumerable.Empty<int>();
                }
            }

            if (country.Eq == null)
            {
                return _id2AccId.SelectMany(x => x.Value);
            }
            int countryId = countries.Get(country.Eq);

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
            int countryId = countries.Get(country.Country);

            if (_id2AccId.ContainsKey(countryId))
            {
                return _id2AccId[countryId];
            }
            else
            {
                return Enumerable.Empty<int>();
            }
        }

        public void FillGroups(List<Group> groups)
        {
            if (groups.Count == 0)
            {
                groups.AddRange(_id2AccId.Keys.Select(x => new Group(countryId: x)));
                groups.Add(new Group());
            }
            else
            {
                int size = groups.Count;
                for (int i = 0; i < size; i++)
                {
                    foreach(var key in _id2AccId.Keys)
                    {
                        Group g = groups[i].Copy();
                        g.CountryId = key;
                        groups.Add(g);
                    }
                }
            }
        }
        
        public bool Contains(int? countryId, int id)
        {
            if (countryId.HasValue)
            {
                List<int> ids;
                if (_id2AccId.TryGetValue(countryId.Value, out ids))
                {
                    return ids.BinarySearch(id) >= 0;
                }
                else
                {
                    return false;
                }
            }

            foreach (var ids in _id2AccId.Values)
            {
                if (ids.BinarySearch(id) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        public void GetByCountryId(
            int? countryId,
            HashSet<int> currentIds,
            IdStorage ids)
        {
            if (countryId.HasValue)
            {
                currentIds.UnionWith(_id2AccId[countryId.Value]);
            }
            else
            {
                currentIds.UnionWith(ids.Except(_id2AccId.SelectMany(x => x.Value)));
            }
        }

        public void LoadBatch(IEnumerable<BatchEntry<int>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach (var entry in batch)
            {
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
    }
}
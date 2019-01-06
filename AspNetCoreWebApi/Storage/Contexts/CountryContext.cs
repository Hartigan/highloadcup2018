using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class CountryContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, HashSet<int>> _id2AccId = new SortedDictionary<int, HashSet<int>>();

        public CountryContext()
        {
        }

        public void Add(int id, int countryId)
        {
            _rw.AcquireWriterLock(2000);
            if (_id2AccId.ContainsKey(countryId))
            {
                _id2AccId[countryId].Add(id);
            }
            else
            {
                _id2AccId[countryId] = new HashSet<int>() { id };
            }
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, int countryId)
        {
            _rw.AcquireWriterLock(2000);
            foreach(var value in _id2AccId.Values)
            {
                value.Remove(id);
            }
            if (_id2AccId.ContainsKey(countryId))
            {
                _id2AccId[countryId].Add(id);
            }
            else
            {
                _id2AccId[countryId] = new HashSet<int>() { id };
            }
            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out int countryId)
        {
            countryId = 0;
            bool res = false;
            _rw.AcquireReaderLock(2000);
            foreach(var pair in _id2AccId)
            {
                if (pair.Value.Contains(id))
                {
                    res = true;
                    countryId = pair.Key;
                    break;
                }
            }
            _rw.ReleaseReaderLock();
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
                HashSet<int> ids;
                if (_id2AccId.TryGetValue(countryId.Value, out ids))
                {
                    return ids.Contains(id);
                }
                else
                {
                    return false;
                }
            }

            return !_id2AccId.Values.SelectMany(x => x).Contains(id);
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
    }
}
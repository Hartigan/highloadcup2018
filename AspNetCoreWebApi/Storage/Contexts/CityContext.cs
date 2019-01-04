using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class CityContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, HashSet<int>> _id2AccId = new SortedDictionary<int, HashSet<int>>();

        public CityContext()
        {
        }

        public void Add(int id, int cityId)
        {
            _rw.AcquireWriterLock(2000);
            if (_id2AccId.ContainsKey(cityId))
            {
                _id2AccId[cityId].Add(id);
            }
            else
            {
                _id2AccId[cityId] = new HashSet<int>() { id };
            }
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, int cityId)
        {
            _rw.AcquireWriterLock(2000);
            foreach (var value in _id2AccId.Values)
            {
                value.Remove(id);
            }
            _id2AccId[cityId].Add(id);
            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out int cityId)
        {
            cityId = 0;
            bool res = false;
            _rw.AcquireReaderLock(2000);
            foreach (var pair in _id2AccId)
            {
                if (pair.Value.Contains(id))
                {
                    res = true;
                    cityId = pair.Key;
                    break;
                }
            }
            _rw.ReleaseReaderLock();
            return res;
        }

        public IEnumerable<int> Filter(
            FilterRequest.CityRequest city,
            IdStorage ids,
            CityStorage cities)
        {
            if (city.IsNull.HasValue)
            {
                if (city.IsNull.Value)
                {
                    return (city.Eq == null && city.Any == null)
                        ? ids.Except(_id2AccId.SelectMany(x => x.Value))
                        : Enumerable.Empty<int>();
                }
            }

            if (city.Eq != null && city.Any != null)
            {
                if (city.Any.Contains(city.Eq))
                {
                    int cityId = cities.Get(city.Eq);
                    return _id2AccId.ContainsKey(cityId) ? _id2AccId[cityId] : Enumerable.Empty<int>();
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (city.Eq != null)
            {
                int cityId = cities.Get(city.Eq);
                return _id2AccId.ContainsKey(cityId) ? _id2AccId[cityId] : Enumerable.Empty<int>();
            }
            else if (city.Any != null)
            {
                HashSet<int> cityIds = new HashSet<int>(city.Any.Select(x => cities.Get(x)));
                return _id2AccId.Where(x => cityIds.Contains(x.Key)).SelectMany(x => x.Value);
            }
            else
            {
                return _id2AccId.SelectMany(x => x.Value);
            }
        }

        public IEnumerable<int> Filter(
            GroupRequest.CityRequest city,
            CityStorage cities)
        {
            int cityId = cities.Get(city.City);

            if (_id2AccId.ContainsKey(cityId))
            {
                return _id2AccId[cityId];
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
                groups.AddRange(_id2AccId.Keys.Select(x => new Group(cityId: x)));
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
                        g.CityId = key;
                        groups.Add(g);
                    }
                }
            }
        }

        public bool Contains(int? cityId, int id)
        {
            if (cityId.HasValue)
            {
                HashSet<int> ids;
                if (_id2AccId.TryGetValue(cityId.Value, out ids))
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

        public void GetByCityId(
            int? cityId,
            HashSet<int> currentIds, 
            IdStorage ids)
        {
            if (cityId.HasValue)
            {
                currentIds.UnionWith(_id2AccId[cityId.Value]);
            }
            else
            {
                currentIds.UnionWith(ids.Except(_id2AccId.SelectMany(x => x.Value)));
            }
        }
    }
}
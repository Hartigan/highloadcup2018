using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class CityContext : IBatchLoader<int>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, List<int>> _id2AccId = new SortedDictionary<int, List<int>>();

        public CityContext()
        {
        }

        public void Add(int id, int cityId)
        {
            _rw.AcquireWriterLock(2000);
            if (_id2AccId.ContainsKey(cityId))
            {
                var list = _id2AccId[cityId];
                list.Insert(~list.BinarySearch(id), id);
            }
            else
            {
                _id2AccId[cityId] = new List<int>() { id };
            }
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, int cityId)
        {
            _rw.AcquireWriterLock(2000);
            foreach (var list in _id2AccId.Values)
            {
                int index = list.BinarySearch(id);
                if (index >= 0)
                {
                    list.RemoveAt(index);
                    break;
                }
            }

            Add(id, cityId);

            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out int cityId)
        {
            cityId = 0;
            bool res = false;
            foreach (var pair in _id2AccId)
            {
                int index = pair.Value.BinarySearch(id);
                if (index >= 0)
                {
                    res = true;
                    cityId = pair.Key;
                    break;
                }
            }
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
                    return (city.Eq == null && city.Any.Count == 0)
                        ? ids.Except(_id2AccId.SelectMany(x => x.Value))
                        : Enumerable.Empty<int>();
                }
            }

            if (city.Eq != null && city.Any.Count > 0)
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
            else if (city.Any.Count > 0)
            {
                return city.Any
                    .Select(x => cities.Get(x))
                    .Where(x => _id2AccId.ContainsKey(x))
                    .SelectMany(x => _id2AccId[x]);
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
                        Group g = groups[i];
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
                List<int> ids;
                if (_id2AccId.TryGetValue(cityId.Value, out ids))
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

        public void LoadBatch(IEnumerable<BatchEntry<int>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach(var entry in batch)
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

            foreach(var cityId in batch.Select(x => x.Value).Distinct())
            {
                _id2AccId[cityId].Sort();
            }

            _rw.ReleaseWriterLock();
        }

        public void Compress()
        {
            foreach (var list in _id2AccId.Values)
            {
                list.Compress();
            }
        }
    }
}
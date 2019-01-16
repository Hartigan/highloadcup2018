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
    public class CityContext : IBatchLoader<short>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private short?[] _raw = new short?[DataConfig.MaxId];
        private Dictionary<short, List<int>> _id2AccId = new Dictionary<short, List<int>>();
        private readonly HashSet<int> _null = new HashSet<int>();

        public CityContext()
        {
        }

        public void Add(int id, short cityId)
        {
            _rw.AcquireWriterLock(2000);
            _raw[id] = cityId;
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

        public void AddOrUpdate(int id, short cityId)
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

        public bool TryGet(int id, out short cityId)
        {
            if (_raw[id].HasValue)
            {
                cityId = _raw[id].Value;
                return true;
            }
            else
            {
                cityId = default(short);
                return false;
            }
        }

        public short? Get(int id)
        {
            return _raw[id];
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
                        ? _null
                        : Enumerable.Empty<int>();
                }
            }

            if (city.Eq != null && city.Any.Count > 0)
            {
                if (city.Any.Contains(city.Eq))
                {
                    short cityId = cities.Get(city.Eq);
                    return _id2AccId.ContainsKey(cityId) ? _id2AccId[cityId] : Enumerable.Empty<int>();
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (city.Eq != null)
            {
                short cityId = cities.Get(city.Eq);
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
            short cityId = cities.Get(city.City);

            if (_id2AccId.ContainsKey(cityId))
            {
                return _id2AccId[cityId];
            }
            else
            {
                return Enumerable.Empty<int>();
            }
        }

        public bool Contains(short? cityId, int id)
        {
            return _raw[id] == cityId;
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            _null.UnionWith(ids.AsEnumerable());
            _null.ExceptWith(_id2AccId.Values.SelectMany(x => x));
            _null.TrimExcess();
        }

        public void GetByCityId(
            short? cityId,
            HashSet<int> currentIds, 
            IdStorage ids)
        {
            if (cityId.HasValue)
            {
                currentIds.UnionWith(_id2AccId[cityId.Value]);
            }
            else
            {
                currentIds.UnionWith(_null);
            }
        }

        public void LoadBatch(IEnumerable<BatchEntry<short>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach(var entry in batch)
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

            foreach(var cityId in batch.Select(x => x.Value).Distinct())
            {
                _id2AccId[cityId].Sort();
            }

            _rw.ReleaseWriterLock();
        }

        public void Compress()
        {
            _null.TrimExcess();
            foreach (var list in _id2AccId.Values)
            {
                list.Compress();
            }
        }
    }
}
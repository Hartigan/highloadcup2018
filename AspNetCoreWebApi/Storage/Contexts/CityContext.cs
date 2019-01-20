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
    public class CityContext : IBatchLoader<short>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private short?[] _raw = new short?[DataConfig.MaxId];
        private Dictionary<short, List<int>> _id2AccId = new Dictionary<short, List<int>>();
        private List<int> _null = new List<int>();
        private List<int> _ids = new List<int>();

        public CityContext()
        {
        }

        public void Add(int id, short cityId)
        {
            _rw.AcquireWriterLock(2000);
            _raw[id] = cityId;
            _ids.SortedInsert(id);
            if (!_id2AccId.ContainsKey(cityId))
            {
                _id2AccId[cityId] = new List<int>();
            }
            _id2AccId[cityId].SortedInsert(id);
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, short cityId)
        {
            _rw.AcquireWriterLock(2000);
            foreach (var list in _id2AccId.Values)
            {
                list.SortedRemove(id);
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
                return MergeSort(city.Any.Select(x => cities.Get(x)).Where(x => _id2AccId.ContainsKey(x)));
            }
            else
            {
                return _ids;
            }
        }

        private IEnumerable<int> MergeSort(IEnumerable<short> cities)
        {
            List<IEnumerator<int>> enumerators = cities.Select(x => _id2AccId[x].AsEnumerable().GetEnumerator()).ToList();

            for (int i = 0; i < enumerators.Count;)
            {
                if (!enumerators[i].MoveNext())
                {
                    enumerators.RemoveAt(i);
                }
            }

            while (enumerators.Count > 0)
            {
                int maxIndex = 0;
                for (int i = 1; i < enumerators.Count; i++)
                {
                    if (enumerators[maxIndex].Current < enumerators[i].Current)
                    {
                        maxIndex = i;
                    }
                }

                yield return enumerators[maxIndex].Current;

                if (!enumerators[maxIndex].MoveNext())
                {
                    enumerators.RemoveAt(maxIndex);
                }
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

            foreach(var entry in batch)
            {
                _raw[entry.Id] = entry.Value;
                if (!_id2AccId.ContainsKey(entry.Value))
                {
                    _id2AccId[entry.Value] = new List<int>();
                }
                _id2AccId[entry.Value].Add(entry.Id);
            }

            foreach (var cityId in batch.Select(x => x.Value).Distinct())
            {
                _id2AccId[cityId].Sort();
                _id2AccId[cityId].TrimExcess();
            }

            _rw.ReleaseWriterLock();
        }

        public void Compress()
        {
        }
    }
}
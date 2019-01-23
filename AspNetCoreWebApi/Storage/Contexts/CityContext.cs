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
        private short[] _raw = new short[DataConfig.MaxId];
        private List<int>[] _id2AccId = new List<int>[1000];
        private List<int> _null = new List<int>();
        private List<int> _ids = new List<int>();

        public CityContext()
        {
        }

        public void Add(int id, short cityId)
        {
            _rw.AcquireWriterLock(2000);
            if (_raw[id] == 0)
            {
                _ids.SortedInsert(id);
            }
            _raw[id] = cityId;

            if (_id2AccId[cityId] == null)
            {
                _id2AccId[cityId] = new List<int>();
            }
            _id2AccId[cityId].SortedInsert(id);
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, short cityId)
        {
            _rw.AcquireWriterLock(2000);

            for(int i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null && _id2AccId[i].SortedRemove(id))
                {
                    break;
                }
            }

            Add(id, cityId);

            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out short cityId)
        {
            if (_raw[id] > 0)
            {
                cityId = _raw[id];
                return true;
            }
            else
            {
                cityId = default(short);
                return false;
            }
        }

        public short Get(int id)
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
                    return _id2AccId[cityId] ?? Enumerable.Empty<int>();
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (city.Eq != null)
            {
                short cityId = cities.Get(city.Eq);
                return _id2AccId[cityId] ?? Enumerable.Empty<int>();
            }
            else if (city.Any.Count > 0)
            {
                return MergeSort(city.Any.Select(x => cities.Get(x)).Where(x => _id2AccId[x] != null));
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
                else
                {
                    i++;
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

            if (_id2AccId[cityId] != null)
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
                if (_raw[id] == 0)
                {
                    _null.Add(id);
                }
            }
            _null.Sort(ReverseComparer<int>.Default);
        }

        public void LoadBatch(int id, short cityId)
        {
            _raw[id] = cityId;
            _ids.Add(id);
            if (_id2AccId[cityId] == null)
            {
                _id2AccId[cityId] = new List<int>();
            }
            _id2AccId[cityId].Add(id);
        }

        public void Compress()
        {
            _ids.Sort(ReverseComparer<int>.Default);

            for(int i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] == null)
                {
                    continue;
                }

                _id2AccId[i].Sort(ReverseComparer<int>.Default);
                _id2AccId[i].TrimExcess();
            }
        }

        public IEnumerable<SingleKeyGroup<short>> GetGroups()
        {
            yield return new SingleKeyGroup<short>(0, _null, _null.Count);
            for(short i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null)
                {
                    yield return new SingleKeyGroup<short>(i, _id2AccId[i], _id2AccId[i].Count);
                }
            }
        }
    }
}
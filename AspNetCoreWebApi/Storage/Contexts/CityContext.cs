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
        private DelaySortedList[] _id2AccId = new DelaySortedList[1000];
        private DelaySortedList _null = new DelaySortedList();
        private DelaySortedList _ids = new DelaySortedList();

        public CityContext()
        {
        }

        public void Add(int id, short cityId)
        {
            _rw.AcquireWriterLock(2000);
            if (_raw[id] == 0)
            {
                _ids.DelayAdd(id);
            }
            _raw[id] = cityId;

            if (_id2AccId[cityId] == null)
            {
                _id2AccId[cityId] = new DelaySortedList();
            }
            _id2AccId[cityId].DelayAdd(id);
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, short cityId)
        {
            _rw.AcquireWriterLock(2000);

            if (_raw[id] > 0)
            {
                _id2AccId[_raw[id]].DelayRemove(id);
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
                        ? _null.AsEnumerable()
                        : Enumerable.Empty<int>();
                }
            }

            if (city.Eq != null && city.Any.Count > 0)
            {
                if (city.Any.Contains(city.Eq))
                {
                    short cityId = cities.Get(city.Eq);
                    return _id2AccId[cityId]?.AsEnumerable() ?? Enumerable.Empty<int>();
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (city.Eq != null)
            {
                short cityId = cities.Get(city.Eq);
                return _id2AccId[cityId]?.AsEnumerable() ?? Enumerable.Empty<int>();
            }
            else if (city.Any.Count > 0)
            {
                return ListHelper
                    .MergeSort(city.Any
                    .Select(x => cities.Get(x))
                    .Where(x => _id2AccId[x] != null)
                    .Select(x => _id2AccId[x].AsEnumerable().GetEnumerator())
                    .ToList());
            }
            else
            {
                return _ids.AsEnumerable();
            }
        }

        public IEnumerable<int> Filter(
            GroupRequest.CityRequest city,
            CityStorage cities)
        {
            short cityId = cities.Get(city.City);

            if (_id2AccId[cityId] != null)
            {
                return _id2AccId[cityId].AsEnumerable();
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
                    _null.Load(id);
                }
            }
            _null.LoadEnded();
        }

        public void LoadBatch(int id, short cityId)
        {
            _raw[id] = cityId;
            _ids.Load(id);
            if (_id2AccId[cityId] == null)
            {
                _id2AccId[cityId] = new DelaySortedList();
            }
            _id2AccId[cityId].Load(id);
        }

        public void Compress()
        {
            _rw.AcquireWriterLock(2000);
            _ids.Flush();

            for(int i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] == null)
                {
                    continue;
                }

                _id2AccId[i].Flush();
            }
            _rw.ReleaseWriterLock();
        }

        public IEnumerable<SingleKeyGroup<short>> GetGroups()
        {
            yield return new SingleKeyGroup<short>(0, _null.AsEnumerable(), _null.Count);
            for(short i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null && _id2AccId[i].Count > 0)
                {
                    yield return new SingleKeyGroup<short>(i, _id2AccId[i].AsEnumerable(), _id2AccId[i].Count);
                }
            }
        }

        public void LoadEnded()
        {
            _ids.LoadEnded();

            for(int i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] == null)
                {
                    continue;
                }

                _id2AccId[i].LoadEnded();
            }
        }
    }
}
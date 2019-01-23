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
        private short[] _raw = new short[DataConfig.MaxId];
        private CountSet[] _id2AccId = new CountSet[200];
        private CountSet _null = new CountSet();
        private CountSet _ids = new CountSet();

        public CountryContext()
        {
        }

        public void Add(int id, short countryId)
        {
            _rw.AcquireWriterLock(2000);
            _raw[id] = countryId;
            _ids.Add(id);
            if (_id2AccId[countryId] == null)
            {
                _id2AccId[countryId] = new CountSet();
            }
            _id2AccId[countryId].Add(id);
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, short countryId)
        {
            _rw.AcquireWriterLock(2000);
            for(int i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] == null)
                {
                    continue;
                }
                _id2AccId[i].Remove(id);
            }

            Add(id, countryId);

            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out short countryId)
        {
            if (_raw[id] > 0)
            {
                countryId = _raw[id];
                return true;
            }
            else
            {
                countryId = 0;
                return false;
            }
        }

        public short Get(int id)
        {
            return _raw[id];
        }

        public IFilterSet Filter(
            FilterRequest.CountryRequest country,
            IdStorage ids,
            CountryStorage countries)
        {
            if (country.IsNull.HasValue)
            {
                if (country.IsNull.Value)
                {
                    return country.Eq == null
                        ? (IFilterSet)_null
                        : FilterSet.Empty;
                }
            }

            if (country.Eq == null)
            {
                return _ids;
            }
            short countryId = countries.Get(country.Eq);

            if (_id2AccId[countryId] != null)
            {
                return _id2AccId[countryId];
            }
            else
            {
                return FilterSet.Empty;
            }
        }

        public IFilterSet Filter(
            GroupRequest.CountryRequest country,
            CountryStorage countries)
        {
            short countryId = countries.Get(country.Country);

            if (_id2AccId[countryId] != null)
            {
                return _id2AccId[countryId];
            }
            else
            {
                return FilterSet.Empty;
            }
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
                if (_raw[id] == 0)
                {
                    _null.Add(id);
                }
            }
        }

        public void LoadBatch(int id, short countryId)
        {
            _raw[id] = countryId;
            _ids.Add(id);
            if (_id2AccId[countryId] == null)
            {
                _id2AccId[countryId] = new CountSet();
            }
            _id2AccId[countryId].Add(id);
        }

        public IEnumerable<SingleKeyGroup<short>> GetGroups()
        {
            yield return new SingleKeyGroup<short>(0, _null.AsEnumerable(), _null.Count);
            for(short i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null)
                {
                    yield return new SingleKeyGroup<short>(i, _id2AccId[i].AsEnumerable(), _id2AccId[i].Count);
                }
            }
        }

        public void Compress()
        {
        }
    }
}
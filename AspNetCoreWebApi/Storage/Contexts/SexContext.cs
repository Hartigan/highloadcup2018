using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class SexContext : IBatchLoader<bool>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private BitArray _raw = new BitArray(DataConfig.MaxId);
        private CountSet[] _id2AccId = new CountSet[2];

        public SexContext()
        {
            _id2AccId[0] = new CountSet();
            _id2AccId[1] = new CountSet();
        }

        public void LoadBatch(int id, bool sex)
        {
            _raw[id] = sex;
            _id2AccId[sex ? 1 : 0].Add(id);
        }

        public void Add(int id, bool sex)
        {
            _rw.AcquireWriterLock(2000);
            _raw[id] = sex;
            _id2AccId[sex ? 1 : 0].Add(id);
            _rw.ReleaseWriterLock();
        }

        public void Update(int id, bool sex)
        {
            _rw.AcquireWriterLock(2000);
            _raw[id] = sex;
            _id2AccId[sex ? 1 : 0].Add(id);
            _id2AccId[sex ? 0 : 1].Remove(id);
            _rw.ReleaseWriterLock();
        }

        public bool Get(int id)
        {
            return _raw[id];
        }

        public IFilterSet Filter(FilterRequest.SexRequest sex)
        {
            if (sex.IsFemale && sex.IsMale)
            {
                return FilterSet.Empty;
            }

            if (sex.IsMale)
            {
                return _id2AccId[1];
            }
            else
            {
                return _id2AccId[0];
            }
        }

        public IFilterSet Filter(GroupRequest.SexRequest sex)
        {
            if (sex.IsFemale && sex.IsMale)
            {
                return FilterSet.Empty;
            }

            if (sex.IsMale)
            {
                return _id2AccId[1];
            }
            else
            {
                return _id2AccId[0];
            }
        }

        public IEnumerable<SingleKeyGroup<bool>> GetGroups()
        {
            yield return new SingleKeyGroup<bool>(false, _id2AccId[0].AsEnumerable(), _id2AccId[0].Count);
            yield return new SingleKeyGroup<bool>(true, _id2AccId[1].AsEnumerable(), _id2AccId[1].Count);
        }

        public bool Contains(bool sex, int id)
        {
            return _raw[id] == sex;
        }

        public void Compress()
        {
        }
    }
}
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
        private FilterSet[] _id2AccId = new FilterSet[2];

        public SexContext()
        {
            _id2AccId[0] = new FilterSet();
            _id2AccId[1] = new FilterSet();
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

        public FilterSet Filter(FilterRequest.SexRequest sex)
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

        public FilterSet Filter(GroupRequest.SexRequest sex)
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

        public void FillGroups(List<Group> groups)
        {
            if (groups.Count == 0)
            {
                groups.Add(new Group(sex: true));
                groups.Add(new Group(sex: false));
            }
            else
            {
                int size = groups.Count;
                for (int i = 0; i < size; i++)
                {
                    Group g = groups[i];
                    g.Sex = false;
                    groups.Add(g);
                    g.Sex = true;
                    groups[i] = g;
                }
            }
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
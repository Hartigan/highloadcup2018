using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class SexContext : IBatchLoader<bool>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private BitArray _raw = new BitArray(DataConfig.MaxId);
        private List<int>[] _id2AccId = new List<int>[2];

        public SexContext()
        {
            _id2AccId[0] = new List<int>();
            _id2AccId[1] = new List<int>();
        }

        public void LoadBatch(IEnumerable<BatchEntry<bool>> batch)
        {
            _rw.AcquireWriterLock(2000);
            _id2AccId[0].AddRange(batch.Where(x => !x.Value).Select(x => x.Id));
            _id2AccId[1].AddRange(batch.Where(x => x.Value).Select(x => x.Id));
            _id2AccId[0].Sort();
            _id2AccId[1].Sort();
            foreach(var entry in batch)
            {
                _raw[entry.Id] = entry.Value;
            }
            _rw.ReleaseWriterLock();
        }

        public void Add(int id, bool sex)
        {
            _rw.AcquireWriterLock(2000);
            _raw[id] = sex;
            var list = _id2AccId[sex ? 1 : 0];
            int index = list.BinarySearch(id);
            list.Insert(~index, id);
            _rw.ReleaseWriterLock();
        }

        public void Update(int id, bool sex)
        {
            _rw.AcquireWriterLock(2000);
            _raw[id] = sex;
            var list = _id2AccId[sex ? 1 : 0];
            int index = list.BinarySearch(id);

            if (index < 0)
            {
                list.Insert(~index, id);
                list = _id2AccId[!sex ? 1 : 0];
                index = list.BinarySearch(id);
                list.RemoveAt(index);
            }
            _rw.ReleaseWriterLock();
        }

        public bool Get(int id)
        {
            return _raw[id];
        }

        public IEnumerable<int> Filter(FilterRequest.SexRequest sex)
        {
            if (sex.IsFemale && sex.IsMale)
            {
                return Enumerable.Empty<int>();
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

        public IEnumerable<int> Filter(GroupRequest.SexRequest sex)
        {
            if (sex.IsFemale && sex.IsMale)
            {
                return Enumerable.Empty<int>();
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

        public void GetBySex(
            bool value,
            HashSet<int> currentIds)
        {
            if (value)
            {
                currentIds.UnionWith(_id2AccId[1]);
            }
            else
            {
                currentIds.UnionWith(_id2AccId[0]);
            }
        }

        public void Compress()
        {
            foreach(var list in _id2AccId)
            {
                list.Compress();
            }
        }
    }
}
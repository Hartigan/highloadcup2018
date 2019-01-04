using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class SexContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private HashSet<int>[] _id2AccId = new HashSet<int>[2];

        public SexContext()
        {
            _id2AccId[0] = new HashSet<int>();
            _id2AccId[1] = new HashSet<int>();
        }

        public void Add(int id, bool sex)
        {
            _rw.AcquireWriterLock(2000);
            _id2AccId[sex ? 1 : 0].Add(id);
            _rw.ReleaseWriterLock();
        }

        public void Update(int id, bool sex)
        {
            _rw.AcquireWriterLock(2000);
            _id2AccId[0].Remove(id);
            _id2AccId[1].Remove(id);
            _id2AccId[sex ? 1 : 0].Add(id);
            _rw.ReleaseWriterLock();
        }

        public bool Get(int id)
        {
            _rw.AcquireReaderLock(2000);
            bool sex = _id2AccId[1].Contains(id);
            _rw.ReleaseReaderLock();
            return sex;
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
                    Group g = groups[i].Copy();
                    groups[i].Sex = true;
                    g.Sex = false;
                    groups.Add(g);
                }
            }
        }

        public bool Contains(bool sex, int id)
        {
            return sex ? _id2AccId[1].Contains(id) : _id2AccId[0].Contains(id);
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
    }
}
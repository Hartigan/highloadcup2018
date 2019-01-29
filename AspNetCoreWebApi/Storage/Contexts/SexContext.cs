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
        private bool[] _raw = new bool[DataConfig.MaxId];
        private CountSet[] _filter = new CountSet[2];
        private DelaySortedList<int>[] _groups = new DelaySortedList<int>[2];

        public SexContext()
        {
            _filter[0] = new CountSet();
            _filter[1] = new CountSet();
            _groups[0] = DelaySortedList<int>.CreateDefault();
            _groups[1] = DelaySortedList<int>.CreateDefault();
        }

        public void LoadBatch(int id, bool sex)
        {
            _raw[id] = sex;
            _filter[sex ? 1 : 0].Add(id);
            _groups[sex ? 1 : 0].Load(id);

        }

        public void Add(int id, bool sex)
        {
            _raw[id] = sex;
            _filter[sex ? 1 : 0].Add(id);
            _groups[sex ? 1 : 0].DelayAdd(id);
        }

        public void Update(int id, bool sex)
        {
            _raw[id] = sex;
            _filter[sex ? 1 : 0].Add(id);
            _filter[sex ? 0 : 1].Remove(id);

            _groups[sex ? 1 : 0].DelayAdd(id);
            _groups[sex ? 0 : 1].DelayRemove(id);
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
                return _groups[1];
            }
            else
            {
                return _groups[0];
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
                return _filter[1];
            }
            else
            {
                return _filter[0];
            }
        }

        public IEnumerable<SingleKeyGroup<bool>> GetGroups()
        {
            yield return new SingleKeyGroup<bool>(false, _groups[0].GetList(), _groups[0].Count);
            yield return new SingleKeyGroup<bool>(true, _groups[1].GetList(), _groups[1].Count);
        }

        public bool Contains(bool sex, int id)
        {
            return _raw[id] == sex;
        }

        public void Compress()
        {
            _groups[0].Flush();
            _groups[1].Flush();
        }

        public void LoadEnded()
        {
            _groups[0].LoadEnded();
            _groups[1].LoadEnded();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class FilterSet : IClearable, IFilterSet
    {
        public static FilterSet Empty { get; } = new FilterSet();

        private BitArray _data = new BitArray(DataConfig.MaxId);

        public void Add(int id)
        {
            _data[id] = true;
        }

        public void Remove(int id)
        {
            _data[id] = false;
        }

        public BitArray GetBitArray()
        {
            return _data;
        }

        public void Add(IEnumerable<int> ids)
        {
            foreach(var id in ids)
            {
                Add(id);
            }
        }

        public void Add(IFilterSet set)
        {
            _data.Or(set.GetBitArray());
        }

        public void IntersectWith(IFilterSet set)
        {
            _data.And(set.GetBitArray());
        }

        public bool Contains(int x)
        {
            return _data[x];
        }

        public void Clear()
        {
            _data.SetAll(false);
        }

        public IEnumerable<int> AsEnumerable()
        {
            return Enumerable.Range(0, DataConfig.MaxId).Where(x => _data[x]);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class FilterSet : IClearable
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

        public void Add(IEnumerable<int> ids)
        {
            foreach(var id in ids)
            {
                Add(id);
            }
        }

        public void Add(FilterSet set)
        {
            _data.Or(set._data);
        }

        public void IntersectWith(FilterSet set)
        {
            _data.And(set._data);
        }

        public bool Contains(int x)
        {
            if (x >= DataConfig.MaxId)
            {
                return false;
            }
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

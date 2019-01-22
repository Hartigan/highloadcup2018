using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class CountSet : IFilterSet
    {
        private BitArray _data = new BitArray(DataConfig.MaxId);
        private int _count = 0;

        public void Add(int id)
        {
            if (!_data[id])
            {
                _count++;
                _data[id] = true;
            }
        }

        public void Remove(int id)
        {
            if (_data[id])
            {
                _count--;
                _data[id] = false;
            }
        }

        public BitArray GetBitArray()
        {
            return _data;
        }

        public bool Contains(int x)
        {
            if (x >= DataConfig.MaxId)
            {
                return false;
            }
            return _data[x];
        }

        public int Count => _count;

        public void Clear()
        {
            _data.SetAll(false);
            _count = 0;
        }

        public IEnumerable<int> AsEnumerable()
        {
            return Enumerable.Range(0, DataConfig.MaxId).Where(x => _data[x]);
        }
    }
}

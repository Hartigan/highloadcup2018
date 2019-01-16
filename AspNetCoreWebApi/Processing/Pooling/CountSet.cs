using System.Collections;
using System.Collections.Generic;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class CountSet : IClearable
    {
        private BitArray _data = new BitArray(DataConfig.MaxId);
        private int _count = 0;

        public void Add(int item)
        {
            if (!_data[item])
            {
                _count++;
            }
        }

        public void UnionWith(IEnumerable<int> items)
        {
            foreach(var item in items)
            {
                Add(item);
            }
        }

        public int Count => _count;

        public void Clear()
        {
            _data.SetAll(false);
            _count = 0;
        }
    }
}

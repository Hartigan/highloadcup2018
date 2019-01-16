using System;
using System.Collections.Generic;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class FilterSet : IClearable
    {
        private byte[] _data = new byte[DataConfig.MaxId];
        private int _count = 0;

        public void IntersectWith(IEnumerable<int> items)
        {
            foreach (var item in items)
            {
                _data[item]++;
            }
            _count++;
        }

        public bool Contains(int x) => _data[x] == _count;

        public void Clear()
        {
            Array.Clear(_data, 0, _data.Length);
            _count = 0;
        }

        public bool Inited => _count > 0;
    }
}

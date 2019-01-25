using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AspNetCoreWebApi.Storage
{
    public class IdStorage
    {
        private bool[] _set = new bool[DataConfig.MaxId];
        public IdStorage()
        {
        }

        public void Add(int item)
        {
            _set[item] = true;
        }

        public bool Contains(int item)
        {
            if (item >= DataConfig.MaxId)
            {
                return false;
            }
            return _set[item];
        }

        public IEnumerable<int> AsEnumerable()
        {
            for (int i = DataConfig.MaxId - 1; i >= 0; i--)
            {
                if (_set[i])
                {
                    yield return i;
                }
            }
        }
    }
}
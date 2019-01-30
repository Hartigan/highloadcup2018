using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class DistinctIterator : IIterator
    {
        private readonly IIterator _iterator;
        private int _current = -1;
        public int Current => _current;
        public bool Completed => _iterator.Completed;

        public DistinctIterator(IIterator iterator)
        {
            _iterator = iterator;
        }

        public bool MoveNext(int item)
        {
            while (_iterator.MoveNext(item) && _current - _iterator.Current == 0)
            {
            }

            if (_iterator.Completed)
            {
                return false;
            }

            _current = _iterator.Current;
            return true;
        }
        public void Reset()
        {
            _iterator.Reset();
        }
    }
}
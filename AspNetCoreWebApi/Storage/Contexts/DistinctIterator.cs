using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class DistinctIterator : IIterator<int>
    {
        private readonly IIterator<int> _iterator;
        private int _current = -1;
        public int Current => _current;
        public IComparer<int> Comparer => _iterator.Comparer;
        public bool Completed => _iterator.Completed;

        public DistinctIterator(IIterator<int> iterator)
        {
            _iterator = iterator;
        }

        public bool MoveNext(int item)
        {
            while (_iterator.MoveNext(item) && _iterator.Comparer.Compare(_iterator.Current, _current) == 0)
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
using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class SortedListIterator<T> : IIterator<T>
    {
        private int _current = -1;
        private readonly IComparer<T> _comparer;
        private readonly List<T> _list;

        public SortedListIterator(List<T> list, IComparer<T> comparer)
        {
            _list = list;
            _comparer = comparer;
        }

        public T Current => _list[_current];

        public IComparer<T> Comparer => _comparer;

        public bool Completed => _current == _list.Count;
        public bool MoveNext(T item)
        {
            int startSearch = _current + 1;

            if (startSearch == _list.Count)
            {
                _current = startSearch;
                return false;
            }

            if (_comparer.Compare(_list[startSearch], item) >= 0)
            {
                _current = startSearch;
                return true;
            }

            int index = _list.BinarySearch(startSearch, _list.Count - startSearch, item, _comparer);
            if (index < 0)
            {
                index = ~index;
            }

            _current = index;
            if (index == _list.Count)
            {
                return false;
            }

            return true;
        }

        public void Reset()
        {
            _current = -1;
        }
    }
}
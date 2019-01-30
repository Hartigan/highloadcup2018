using System.Collections.Generic;
using AspNetCoreWebApi.Processing;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class SortedListIterator : IIterator
    {
        private int _current = -1;
        private readonly List<int> _list;

        public SortedListIterator(List<int> list)
        {
            _list = list;
        }

        public int Current => _list[_current];
        public bool Completed => _current == _list.Count;
        public bool MoveNext(int item)
        {
            int startSearch = _current + 1;

            if (startSearch == _list.Count)
            {
                _current = startSearch;
                return false;
            }

            if (item - _list[startSearch] >= 0)
            {
                _current = startSearch;
                return true;
            }

            int index = _list.BinarySearch(startSearch, _list.Count - startSearch, item, ReverseComparer<int>.Default);
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
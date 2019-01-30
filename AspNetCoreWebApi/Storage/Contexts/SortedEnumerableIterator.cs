using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class SortedEnumerableIterator<T> : IIterator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly IComparer<T> _comparer;
        public T Current => _enumerator.Current;
        public IComparer<T> Comparer => _comparer;
        private bool _completed;
        public bool Completed => _completed;

        public SortedEnumerableIterator(IEnumerable<T> enumerable, IComparer<T> comparer)
        {
            _enumerator = enumerable.GetEnumerator();
            _comparer = comparer;
        }

        public bool MoveNext(T item)
        {
            while(_enumerator.MoveNext())
            {
                if (_comparer.Compare(item, _enumerator.Current) <= 0)
                {
                    return true;
                }
            }
            _completed = true;
            return false;
        }

        public void Reset()
        {
            _completed = false;
            _enumerator.Reset();
        }
    }
}
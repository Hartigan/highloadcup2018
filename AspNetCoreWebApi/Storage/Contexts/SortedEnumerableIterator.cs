using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class SortedEnumerableIterator : IIterator
    {
        private readonly IEnumerator<int> _enumerator;
        public int Current => _enumerator.Current;
        private bool _completed;
        public bool Completed => _completed;

        public SortedEnumerableIterator(IEnumerable<int> enumerable)
        {
            _enumerator = enumerable.GetEnumerator();
        }

        public bool MoveNext(int item)
        {
            while(_enumerator.MoveNext())
            {
                if (_enumerator.Current - item <= 0)
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
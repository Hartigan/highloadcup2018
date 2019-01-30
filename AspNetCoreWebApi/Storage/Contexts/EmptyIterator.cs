using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class EmptyIterator<T> : IIterator<T>
    {
        public T Current => throw new System.NotImplementedException();
        public IComparer<T> Comparer => _comparer;
        public bool Completed => true;

        private readonly IComparer<T> _comparer;

        public EmptyIterator(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public bool MoveNext(T item)
        {
            return false;
        }
        public void Reset()
        {
        }
    }
}
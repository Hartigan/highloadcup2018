using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public interface IIterator<T>
    {
        T Current { get; }
        bool MoveNext(T item);
        IComparer<T> Comparer { get; }
        bool Completed { get; }
        void Reset();
    }
}
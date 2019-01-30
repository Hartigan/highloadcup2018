using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public interface IIterator
    {
        int Current { get; }
        bool MoveNext(int item);
        bool Completed { get; }
        void Reset();
    }
}
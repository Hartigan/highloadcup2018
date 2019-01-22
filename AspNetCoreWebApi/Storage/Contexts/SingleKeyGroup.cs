using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public struct SingleKeyGroup<T>
    {
        public SingleKeyGroup(
            T key,
            IEnumerable<int> ids,
            int count)
        {
            Key = key;
            Ids = ids;
            Count = count;
        }

        public T Key;
        public IEnumerable<int> Ids;
        public int Count;
    }
}
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;


namespace AspNetCoreWebApi.Processing.Pooling
{
    public class HashSetPolicy<T> : IPooledObjectPolicy<HashSet<T>>
    {
        public HashSet<T> Create()
        {
            return new HashSet<T>(128);
        }

        public bool Return(HashSet<T> obj)
        {
            obj.Clear();
            return true;
        }
    }
}

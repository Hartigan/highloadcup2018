using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;


namespace AspNetCoreWebApi.Processing.Pooling
{
    public class ListPolicy<T> : IPooledObjectPolicy<List<T>>
    {
        public List<T> Create()
        {
            return new List<T>(128);
        }

        public bool Return(List<T> obj)
        {
            obj.Clear();
            return true;
        }
    }
}

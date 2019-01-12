using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;


namespace AspNetCoreWebApi.Processing.Pooling
{
    public class DictionaryPolicy<TKey, TValue> : IPooledObjectPolicy<Dictionary<TKey, TValue>>
    {
        public Dictionary<TKey, TValue> Create()
        {
            return new Dictionary<TKey, TValue>(128);
        }

        public bool Return(Dictionary<TKey, TValue> obj)
        {
            obj.Clear();
            return true;
        }
    }
}

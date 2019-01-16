using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class MyPool<T> : ObjectPool<T> where T : class
    {
        private readonly IPooledObjectPolicy<T> _policy;
        private readonly ConcurrentQueue<T> _pool = new ConcurrentQueue<T>();

        public MyPool(IPooledObjectPolicy<T> policy)
        {
            _policy = policy;
        }

        public override T Get()
        {
            T obj;
            if (_pool.TryDequeue(out  obj))
            {
                return obj;
            }
            else
            {
                return _policy.Create();
            }
        }

        public override void Return(T obj)
        {
            if (_policy.Return(obj))
            {
                _pool.Enqueue(obj);
            }

        }
    }
}

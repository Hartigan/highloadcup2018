using Microsoft.Extensions.ObjectPool;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class GenericPolicy<T> : IPooledObjectPolicy<T> where T : IClearable, new()
    {
        public T Create()
        {
            return new T();
        }

        public bool Return(T obj)
        {
            obj.Clear();
            return true;
        }
    }
}
using System;
using Microsoft.Extensions.ObjectPool;


namespace AspNetCoreWebApi.Processing.Pooling
{
    public class FilterArrayPolicy : IPooledObjectPolicy<byte[]>
    {
        public byte[] Create()
        {
            return new byte[DataConfig.MaxId];
        }

        public bool Return(byte[] obj)
        {
            Array.Clear(obj, 0, obj.Length);
            return true;
        }
    }
}

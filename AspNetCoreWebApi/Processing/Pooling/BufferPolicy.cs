using System;
using Microsoft.Extensions.ObjectPool;


namespace AspNetCoreWebApi.Processing.Pooling
{
    public class BufferPolicy : IPooledObjectPolicy<byte[]>
    {
        public byte[] Create()
        {
            return new byte[8 * 1024];
        }

        public bool Return(byte[] obj)
        {
            return true;
        }
    }
}

using System;
using System.Threading;

namespace AspNetCoreWebApi.Storage
{
    class IdGenerator
    {
        private volatile int _last = 0;

        public int Get()
        {
            return Interlocked.Increment(ref _last);
        }
    }
}
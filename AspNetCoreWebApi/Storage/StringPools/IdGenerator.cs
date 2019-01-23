using System;
using System.Threading;

namespace AspNetCoreWebApi.Storage.StringPools
{
    class IdGenerator
    {
        private volatile int _last = 1;

        public short Get()
        {
            return (short)Interlocked.Increment(ref _last);
        }
    }
}
using System;

namespace AspNetCoreWebApi.Domain
{
    public struct Premium
    {
        public Premium(DateTimeOffset start, DateTimeOffset finish)
        {
            Start = start;
            Finish = finish;
        }

        public DateTimeOffset Start;
        public DateTimeOffset Finish;
    }
}
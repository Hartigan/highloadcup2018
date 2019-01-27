using System;

namespace AspNetCoreWebApi.Domain
{
    public struct Premium
    {
        public Premium(UnixTime start, UnixTime finish)
        {
            Start = start;
            Finish = finish;
        }

        public UnixTime Start;
        public UnixTime Finish;

        public bool IsNotEmpty() => !Start.IsEmpty() || !Finish.IsEmpty();
    }
}
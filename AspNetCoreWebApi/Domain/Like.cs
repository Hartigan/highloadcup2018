using System;

namespace AspNetCoreWebApi.Domain
{

    public struct Like
    {
        public Like(int likeeId, int likerId, UnixTime ts)
        {
            LikeeId = likeeId;
            LikerId = likerId;
            Timestamp = ts;
        }

        public int LikeeId;
        public int LikerId;
        public UnixTime Timestamp;
    }
}
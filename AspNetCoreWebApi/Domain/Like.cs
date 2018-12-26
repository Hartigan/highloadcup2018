using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetCoreWebApi.Domain
{

    public struct Like
    {
        public Like(int likeeId, int likerId, DateTimeOffset ts)
        {
            LikeeId = likeeId;
            LikerId = likerId;
            Timestamp = ts;
        }

        public int LikeeId;
        public int LikerId;
        public DateTimeOffset Timestamp;
    }
}
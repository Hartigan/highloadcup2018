using System;
using AspNetCoreWebApi.Domain;

namespace AspNetCoreWebApi.Processing
{
    public static class DataConfig
    {
        public static int MaxId = 1400000;

        public static UnixTime Now { get; set; }

        public static int NowSeconds { get; set; }

        public static bool DataUpdates { get; set; }

        public static bool LikesUpdates { get; set; }

        public static bool GroupUpdates { get; set; }

        public static bool IsNow(this Premium p)
        {
            return p.Finish > Now && p.Start < Now;
        }
    }
}
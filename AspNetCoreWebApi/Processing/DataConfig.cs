using System;

namespace AspNetCoreWebApi.Processing
{
    public static class DataConfig
    {
        public static DateTimeOffset Now { get; set; }

        public static int NowSeconds { get; set; }
    }
}
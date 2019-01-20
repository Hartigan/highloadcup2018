
using System;

namespace AspNetCoreWebApi.Domain
{
    public struct UnixTime
    {
        public int Seconds;

        public UnixTime(int ts)
        {
            Seconds = ts;
        }

        public int Year
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(Seconds).Year;
            }
        }

        public static int operator -(UnixTime left, UnixTime right)
        {
            return left.Seconds - right.Seconds;
        }

        public static bool operator <(UnixTime left, UnixTime right)
        {
            return left.Seconds < right.Seconds;
        }

        public static bool operator >(UnixTime left, UnixTime right)
        {
            return left.Seconds > right.Seconds;
        }

        public static bool operator <=(UnixTime left, UnixTime right)
        {
            return left.Seconds <= right.Seconds;
        }

        public static bool operator >=(UnixTime left, UnixTime right)
        {
            return left.Seconds >= right.Seconds;
        }

        public static bool operator ==(UnixTime left, UnixTime right)
        {
            return left.Seconds == right.Seconds;
        }

        public static bool operator !=(UnixTime left, UnixTime right)
        {
            return left.Seconds != right.Seconds;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UnixTime))
            {
                return false;
            }

            var time = (UnixTime)obj;
            return Seconds == time.Seconds;
        }

        public override int GetHashCode()
        {
            return Seconds;
        }
    }
}

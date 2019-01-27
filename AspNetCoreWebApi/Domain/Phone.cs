using System;

namespace AspNetCoreWebApi.Domain
{
    public struct Phone
    {
        public Phone(short prefix, short code, int suffix)
        {
            Prefix = prefix;
            Code = code;
            Suffix = suffix;
        }

        public short Prefix;

        public short Code;

        public int Suffix;

        public bool IsNotEmpty() => Suffix != 0 || Prefix != 0 || Code != 0;
    }
}
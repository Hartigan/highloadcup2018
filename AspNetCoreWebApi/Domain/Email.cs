using System;

namespace AspNetCoreWebApi.Domain
{
    public struct Email
    {
        public Email(string prefix, short domainId)
        {
            Prefix = string.Intern(prefix);
            DomainId = domainId;
        }

        public string Prefix;

        public short DomainId;
    }
}
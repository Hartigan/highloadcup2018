using System;

namespace AspNetCoreWebApi.Domain
{
    public struct Email
    {
        public Email(string prefix, int domainId)
        {
            Prefix = string.Intern(prefix);
            DomainId = domainId;
        }

        public string Prefix;

        public int DomainId;
    }
}
using System;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage
{
    public class MainStorage
    {
        public IdStorage Ids { get; } = new IdStorage();

        public CityStorage Cities { get; } = new CityStorage();
        
        public CountryStorage Countries { get; } = new CountryStorage();

        public DomainStorage Domains { get; } = new DomainStorage();

        public EmailHashStorage EmailHashes { get; } = new EmailHashStorage();

        public InterestStorage Interests { get; } = new InterestStorage();

        public PhoneHashStorage PhoneHashes { get; } = new PhoneHashStorage();

        public NameStorage Names { get; } = new NameStorage();

        public LastNameStorage LastNames { get; } = new LastNameStorage();

        public MainStorage()
        {
        }
    }
}
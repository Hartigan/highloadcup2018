using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;

namespace AspNetCoreWebApi.Processing.Requests
{
    public class FilterRequest
    {
        public class SexRequest
        {
            public bool IsActive;
            public bool IsMale;

            public bool IsFemale;
        }
        public SexRequest Sex = new SexRequest();

        public class EmailRequest
        {
            public bool IsActive;
            public string Domain;
            public string Lt;
            public string Gt;
        }
        public EmailRequest Email = new EmailRequest();

        public class StatusRequest
        {
            public bool IsActive;
            public Status? Eq;
            public Status? Neq;
        }
        public StatusRequest Status = new StatusRequest();

        public class FnameRequest
        {
            public bool IsActive;
            public string Eq;
            public IReadOnlyList<string> Any;
            public bool? IsNull;
        }
        public FnameRequest Fname = new FnameRequest();

        public class SnameRequest
        {
            public bool IsActive;
            public string Eq;
            public string Starts;
            public bool? IsNull;
        }
        public SnameRequest Sname = new SnameRequest();

        public class PhoneRequest
        {
            public bool IsActive;
            public short? Code;
            public bool? IsNull;
        }
        public PhoneRequest Phone = new PhoneRequest();

        public class CountryRequest
        {
            public bool IsActive;
            public string Eq;
            public bool? IsNull;
        }
        public CountryRequest Country = new CountryRequest();

        public class CityRequest
        {
            public bool IsActive;
            public string Eq;
            public IReadOnlyList<string> Any;
            public bool? IsNull;
        }
        public CityRequest City = new CityRequest();

        public class BirthRequest
        {
            public bool IsActive;
            public DateTimeOffset? Lt;
            public DateTimeOffset? Gt;
            public int? Year;
        }
        public BirthRequest Birth = new BirthRequest();

        public class InterestsRequest
        {
            public bool IsActive;
            public IReadOnlyList<string> Contains;
            public IReadOnlyList<string> Any;
        }
        public InterestsRequest Interests = new InterestsRequest();

        public class LikesRequest
        {
            public bool IsActive;
            public IReadOnlyList<int> Contains;
        }
        public LikesRequest Likes = new LikesRequest();

        public class PremiumRequest
        {
            public bool IsActive;
            public bool Now;
            public bool? IsNull;
        }
        public PremiumRequest Premium = new PremiumRequest();

        public int Limit { get; set; }

        public TaskCompletionSource<IReadOnlyList<int>> TaskComletionSource = new TaskCompletionSource<IReadOnlyList<int>>();
    }
}
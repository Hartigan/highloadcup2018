using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Responses;

namespace AspNetCoreWebApi.Processing.Requests
{
    public class FilterRequest : IClearable
    {
        public class SexRequest
        {
            public bool IsActive;
            public bool IsMale;
            public bool IsFemale;
            public void Clear()
            {
                IsActive = false;
                IsMale = false;
                IsFemale = false;
            }
        }
        public SexRequest Sex = new SexRequest();

        public class EmailRequest
        {
            public bool IsActive;
            public string Domain;
            public string Lt;
            public string Gt;
            public void Clear()
            {
                IsActive = false;
                Domain = null;
                Lt = null;
                Gt = null;
            }
        }
        public EmailRequest Email = new EmailRequest();

        public class StatusRequest
        {
            public bool IsActive;
            public Status? Eq;
            public Status? Neq;
            public void Clear()
            {
                IsActive = false;
                Eq = null;
                Neq = null;
            }
        }
        public StatusRequest Status = new StatusRequest();

        public class FnameRequest
        {
            public bool IsActive;
            public string Eq;
            public HashSet<string> Any = new HashSet<string>();
            public bool? IsNull;
            public void Clear()
            {
                IsActive = false;
                Eq = null;
                Any.Clear();
                IsNull = null;
            }
        }
        public FnameRequest Fname = new FnameRequest();

        public class SnameRequest
        {
            public bool IsActive;
            public string Eq;
            public string Starts;
            public bool? IsNull;
            public void Clear()
            {
                IsActive = false;
                Eq = null;
                Starts = null;
                IsNull = null;
            }
        }
        public SnameRequest Sname = new SnameRequest();

        public class PhoneRequest
        {
            public bool IsActive;
            public short? Code;
            public bool? IsNull;
            public void Clear()
            {
                IsActive = false;
                Code = null;
                IsNull = null;
            }
        }
        public PhoneRequest Phone = new PhoneRequest();

        public class CountryRequest
        {
            public bool IsActive;
            public string Eq;
            public bool? IsNull;
            public void Clear()
            {
                IsActive = false;
                Eq = null;
                IsNull = null;
            }
        }
        public CountryRequest Country = new CountryRequest();

        public class CityRequest
        {
            public bool IsActive;
            public string Eq;
            public HashSet<string> Any = new HashSet<string>();
            public bool? IsNull;
            public void Clear()
            {
                IsActive = false;
                Eq = null;
                Any.Clear();
                IsNull = null;
            }
        }
        public CityRequest City = new CityRequest();

        public class BirthRequest
        {
            public bool IsActive;
            public DateTimeOffset? Lt;
            public DateTimeOffset? Gt;
            public int? Year;
            public void Clear()
            {
                IsActive = false;
                Lt = null;
                Gt = null;
                Year = null;
            }
        }
        public BirthRequest Birth = new BirthRequest();

        public class InterestsRequest
        {
            public bool IsActive;
            public HashSet<string> Contains = new HashSet<string>();
            public HashSet<string> Any = new HashSet<string>();
            public void Clear()
            {
                IsActive = false;
                Contains.Clear();
                Any.Clear();
            }
        }
        public InterestsRequest Interests = new InterestsRequest();

        public class LikesRequest
        {
            public bool IsActive;
            public HashSet<int> Contains = new HashSet<int>();
            public void Clear()
            {
                IsActive = false;
                Contains.Clear();
            }
        }
        public LikesRequest Likes = new LikesRequest();

        public class PremiumRequest
        {
            public bool IsActive;
            public bool Now;
            public bool? IsNull;
            public void Clear()
            {
                IsActive = false;
                Now = false;
                IsNull = null;
            }
        }
        public PremiumRequest Premium = new PremiumRequest();

        public int Limit { get; set; }

        public HashSet<Field> Fields { get; } = new HashSet<Field>();

        public void Clear()
        {
            Fields.Clear();
            Limit = 0;
            Sex.Clear();
            Email.Clear();
            Status.Clear();
            Fname.Clear();
            Sname.Clear();
            Phone.Clear();
            Country.Clear();
            City.Clear();
            Birth.Clear();
            Interests.Clear();
            Likes.Clear();
            Premium.Clear();
        }
    }
}
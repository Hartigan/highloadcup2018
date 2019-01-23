using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Responses;

namespace AspNetCoreWebApi.Processing.Requests
{
    public static class GroupKeyExtensions
    {
        public static bool TryParse(string str, out GroupKey key)
        {
            switch(str)
            {
                case "sex":
                    key = GroupKey.Sex;
                    return true;
                case "status":
                    key = GroupKey.Status;
                    return true;
                case "country":
                    key = GroupKey.Country;
                    return true;
                case "city":
                    key = GroupKey.City;
                    return true;
                case "interests":
                    key = GroupKey.Interest;
                    return true;

                default:
                    key = default(GroupKey);
                    return false;
            }
        }
    }

    [Flags]
    public enum GroupKey : byte
    {
        Empty = 0,
        Sex = 1,
        Status = 2,
        Interest = 4,
        Country = 8,
        City = 16
    }

    public class GroupRequest : IClearable
    {
        public GroupRequest()
        {
        }

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
        public SexRequest Sex { get; } = new SexRequest();

        public class StatusRequest
        {
            public bool IsActive;
            public Status Status;
            public void Clear()
            {
                IsActive = false;
            }
        }
        public StatusRequest Status { get; } = new StatusRequest();

        public class CountryRequest
        {
            public bool IsActive;
            public string Country;
            public void Clear()
            {
                IsActive = false;
                Country = null;
            }
        }
        public CountryRequest Country { get; } = new CountryRequest();

        public class CityRequest
        {
            public bool IsActive;
            public string City;
            public void Clear()
            {
                IsActive = false;
                City = null;
            }
        }
        public CityRequest City { get; } = new CityRequest();

        public class BirthRequest
        {
            public bool IsActive;
            public int Year;
            public void Clear()
            {
                IsActive = false;
            }
        }
        public BirthRequest Birth { get; } = new BirthRequest();

        public class InterestRequest
        {
            public bool IsActive;
            public string Interest;
            public void Clear()
            {
                IsActive = false;
                Interest = null;
            }
        }
        public InterestRequest Interest { get; } = new InterestRequest();

        public class LikeRequest
        {
            public bool IsActive;
            public int Id;
            public void Clear()
            {
                IsActive = false;
            }
        }
        public LikeRequest Like { get; } = new LikeRequest();

        public class JoinedRequest
        {
            public bool IsActive;
            public int Year;
            public void Clear()
            {
                IsActive = false;
            }
        }
        public JoinedRequest Joined { get; } = new JoinedRequest();

        public int Limit { get; set; }
        public bool Order { get; set; }
        public GroupKey Keys { get; set; }
        public List<GroupKey> KeyOrder { get; set; } = new List<GroupKey>();

        public void Clear()
        {
            Sex.Clear();
            Status.Clear();
            Country.Clear();
            City.Clear();
            Birth.Clear();
            Interest.Clear();
            Like.Clear();
            Joined.Clear();
            Limit = 0;
            Order = false;
            Keys = 0;
            KeyOrder.Clear();
        }
    }
}
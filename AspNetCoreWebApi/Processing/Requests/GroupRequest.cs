using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
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

    public enum GroupKey
    {
        Sex,
        Status,
        Interest,
        Country,
        City
    }

    public class GroupRequest
    {
        public GroupRequest()
        {
        }

        public class SexRequest
        {
            public bool IsActive;
            public bool IsMale;
            public bool IsFemale;
        }
        public SexRequest Sex { get; } = new SexRequest();

        public class StatusRequest
        {
            public bool IsActive;
            public Status Status;
        }
        public StatusRequest Status { get; } = new StatusRequest();

        public class CountryRequest
        {
            public bool IsActive;
            public string Country;
        }
        public CountryRequest Country { get; } = new CountryRequest();

        public class CityRequest
        {
            public bool IsActive;
            public string City;
        }
        public CityRequest City { get; } = new CityRequest();

        public class BirthRequest
        {
            public bool IsActive;
            public int Year;
        }
        public BirthRequest Birth { get; } = new BirthRequest();

        public class InterestRequest
        {
            public bool IsActive;
            public string Interest;
        }
        public InterestRequest Interest { get; } = new InterestRequest();

        public class LikeRequest
        {
            public bool IsActive;
            public int Id;
        }
        public LikeRequest Like { get; } = new LikeRequest();

        public class JoinedRequest
        {
            public bool IsActive;
            public int Year;
        }
        public JoinedRequest Joined { get; } = new JoinedRequest();

        public TaskCompletionSource<GroupResponse> TaskCompletionSource { get; } = new TaskCompletionSource<GroupResponse>();
        public int Limit { get; set; }
        public bool Order { get; set; }
        public IReadOnlyList<GroupKey> Keys { get; set; }
    }
}
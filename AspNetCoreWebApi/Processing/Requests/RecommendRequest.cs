using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Responses;

namespace AspNetCoreWebApi.Processing.Requests
{
    public class RecommendRequest
    {
        public RecommendRequest()
        {
        }

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

        public TaskCompletionSource<IReadOnlyList<int>> TaskCompletionSource { get; } = new TaskCompletionSource<IReadOnlyList<int>>();

        public int Id { get; set; }
        public int Limit { get; set; }
    }
}
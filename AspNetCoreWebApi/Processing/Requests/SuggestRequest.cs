using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Responses;

namespace AspNetCoreWebApi.Processing.Requests
{
    public class SuggestRequest : IClearable
    {
        public SuggestRequest()
        {
        }

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

        public int Id { get; set; }
        public int Limit { get; set; }

        public void Clear()
        {
            Country.Clear();
            City.Clear();
            Id = 0;
            Limit = 0;
        }
    }
}
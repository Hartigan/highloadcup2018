using System;
using System.Collections.Generic;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;

namespace AspNetCoreWebApi.Processing.Responses
{
    public class SuggestResponse : IClearable
    {
        public SuggestResponse()
        {
        }

        public List<int> Ids { get; } = new List<int>();

        public int Limit { get; set; }

        public void Clear()
        {
            Ids.Clear();
            Limit = 0;
        }
    }
}
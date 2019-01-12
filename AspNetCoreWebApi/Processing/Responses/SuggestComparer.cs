using System;
using System.Collections.Generic;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;

namespace AspNetCoreWebApi.Processing.Responses
{
    public class SuggestComparer : IComparer<int>, IClearable
    {
        private Dictionary<int, float> _similarity;

        public void Init(Dictionary<int, float> similarity)
        {
            _similarity = similarity;
        }

        public void Clear()
        {
            _similarity = null;
        }

        private static Dictionary<Status, int> _statuses = new Dictionary<Status, int>()
        {
            { Status.Free, 0 },
            { Status.Complicated, 1 },
            { Status.Reserved, 2 }
        };

        public int Compare(int x, int y)
        {
            return Comparer<float>.Default.Compare(_similarity[y], _similarity[x]);
        }
    }
}
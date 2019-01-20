using System;
using System.Collections.Generic;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage;
using AspNetCoreWebApi.Storage.Contexts;

namespace AspNetCoreWebApi.Processing.Responses
{
    public class RecommendComparer : IComparer<int>, IClearable
    {
        private MainContext _context;
        private Dictionary<int, int> _recommeded;
        private int _birth;
        public void Init(
            MainContext context,
            Dictionary<int, int> recommeded,
            int birth)
        {
            _context = context;
            _recommeded = recommeded;
            _birth = birth;
        }

        public void Clear()
        {
            _context = null;
            _recommeded = null;
            _birth = 0;
        }

        private static Dictionary<Status, int> _statuses = new Dictionary<Status, int>()
        {
            { Status.Free, 0 },
            { Status.Complicated, 1 },
            { Status.Reserved, 2 }
        };

        public int Compare(int x, int y)
        {
            bool premiumX = _context.Premiums.IsNow(x);
            bool premiumY = _context.Premiums.IsNow(y);

            if (premiumX != premiumY)
            {
                return premiumX ? -1 : 1;
            }

            Status statusX = _context.Statuses.Get(x);
            Status statusY = _context.Statuses.Get(y);

            if (statusX != statusY)
            {
                return _statuses[statusX] - _statuses[statusY];
            }

            int countX = _recommeded[x];
            int countY = _recommeded[y];

            if (countX != countY)
            {
                return countY - countX;
            }

            int diffX = Math.Abs(_context.Birth.Get(x).Seconds - _birth);
            int diffY = Math.Abs(_context.Birth.Get(y).Seconds - _birth);

            return (diffX - diffY);
        }
    }
}
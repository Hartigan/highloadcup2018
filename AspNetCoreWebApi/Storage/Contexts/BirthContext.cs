using System;
using System.Collections.Generic;
using System.Linq;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class BirthContext : IBatchLoader<UnixTime>, ICompresable
    {
        private UnixTime[] _id2time = new UnixTime[DataConfig.MaxId];
        private Dictionary<int, CountSet> _years = new Dictionary<int, CountSet>(); 

        public BirthContext()
        {
        }

        public void AddOrUpdate(int id, UnixTime time)
        {
            var oldYear = _id2time[id].Year;
            _id2time[id] = time;

            if (_years.ContainsKey(oldYear))
            {
                _years[oldYear].Remove(id);
            }

            var newYear = time.Year;
            if (!_years.ContainsKey(newYear))
            {
                _years[newYear] = new CountSet();
            }

            _years[newYear].Add(id);
        }

        public UnixTime Get(int id) => _id2time[id];

        public IEnumerable<int> Filter(FilterRequest.BirthRequest birth, IdStorage idStorage)
        {
            return idStorage.AsEnumerable().Where(x =>
            {
                UnixTime b = _id2time[x];
                if (birth.Gt.HasValue && b <= birth.Gt.Value)
                {
                    return false;
                }

                if (birth.Lt.HasValue && b >= birth.Lt.Value)
                {
                    return false;
                }

                if (birth.Year.HasValue && b.Year != birth.Year.Value)
                {
                    return false;
                }

                return true;
            });
        }

        public IFilterSet Filter(GroupRequest.BirthRequest birth)
        {
            if (_years.ContainsKey(birth.Year))
            {
                return _years[birth.Year];
            }
            else
            {
                return FilterSet.Empty;
            }
        }

        public void LoadBatch(int id, UnixTime item)
        {
            var newYear = item.Year;
            if (!_years.ContainsKey(newYear))
            {
                _years[newYear] = new CountSet();
            }

            _years[newYear].Add(id);
            _id2time[id] = item;
        }

        public void Compress()
        {
        }
    }
}
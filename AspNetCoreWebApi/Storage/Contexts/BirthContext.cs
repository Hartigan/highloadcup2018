using System;
using System.Collections.Generic;
using System.Linq;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class BirthContext : IBatchLoader<DateTimeOffset>, ICompresable
    {
        private DateTimeOffset?[] _id2time = new DateTimeOffset?[DataConfig.MaxId];

        public BirthContext()
        {
        }

        public void AddOrUpdate(int id, DateTimeOffset time)
        {
            _id2time[id] = time;
        }

        public DateTimeOffset Get(int id) => _id2time[id].Value;

        public IEnumerable<int> Filter(FilterRequest.BirthRequest birth, IdStorage idStorage)
        {
            return idStorage.AsEnumerable().Where(x =>
            {
                DateTimeOffset b = _id2time[x].Value;
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

        public IEnumerable<int> Filter(GroupRequest.BirthRequest birth, IdStorage idStorage)
        {
            return idStorage.AsEnumerable().Where(x => _id2time[x].Value.Year == birth.Year);
        }

        public void LoadBatch(IEnumerable<BatchEntry<DateTimeOffset>> batch)
        {
            foreach(var entry in batch)
            {
                _id2time[entry.Id] = entry.Value;
            }
        }

        public void Compress()
        {
        }
    }
}
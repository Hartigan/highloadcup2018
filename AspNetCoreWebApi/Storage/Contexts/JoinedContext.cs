using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class JoinedContext : IBatchLoader<DateTimeOffset>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private DateTimeOffset?[] _id2time = new DateTimeOffset?[DataConfig.MaxId];

        public JoinedContext()
        {
        }

        public void AddOrUpdate(int id, DateTimeOffset time)
        {
            _id2time[id] = time;
        }

        public void Compress()
        {
        }

        public IEnumerable<int> Filter(GroupRequest.JoinedRequest joined, IdStorage idStorage)
        {
            return idStorage.AsEnumerable().Where(x => _id2time[x].Value.Year == joined.Year);
        }

        public void LoadBatch(IEnumerable<BatchEntry<DateTimeOffset>> batch)
        {
            foreach(var entry in batch)
            {
                _id2time[entry.Id] = entry.Value;
            }
        }
    }
}
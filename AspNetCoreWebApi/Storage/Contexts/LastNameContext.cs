using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class LastNameContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, string> _id2name = new SortedDictionary<int, string>();

        public LastNameContext()
        {
        }

        public void AddOrUpdate(int id, string name)
        {
            _rw.AcquireWriterLock(2000);
            _id2name[id] = string.Intern(name);
            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out string sname) => _id2name.TryGetValue(id, out sname);

        public IEnumerable<int> Filter(FilterRequest.SnameRequest sname, IdStorage idStorage)
        {
            if (sname.IsNull != null)
            {
                if (sname.IsNull.Value)
                {
                    if (sname.Eq == null && sname.Starts == null)
                    {
                        return idStorage.Except(_id2name.Keys);
                    }
                    else
                    {
                        return Enumerable.Empty<int>();
                    }
                }
            }

            if (sname.Eq != null && sname.Starts != null)
            {
                if (sname.Eq.StartsWith(sname.Starts))
                {
                    return _id2name.Where(x => x.Value == sname.Eq).Select(x => x.Key);
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (sname.Starts != null)
            {
                return _id2name.Where(x => x.Value.StartsWith(sname.Starts)).Select(x => x.Key);
            }
            else if (sname.Eq != null)
            {
                return _id2name.Where(x => x.Value == sname.Eq).Select(x => x.Key);
            }

            return _id2name.Keys;
        }
    }
}
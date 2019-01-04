using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class FirstNameContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, string> _id2name = new SortedDictionary<int, string>();

        public FirstNameContext()
        {
        }

        public void AddOrUpdate(int id, string name)
        {
            _rw.AcquireWriterLock(2000);
            _id2name[id] = string.Intern(name);
            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out string fname) => _id2name.TryGetValue(id, out fname);

        public IEnumerable<int> Filter(
            FilterRequest.FnameRequest fname,
            IdStorage idStorage)
        {
            if (fname.IsNull != null)
            {
                if (fname.IsNull.Value)
                {
                    if (fname.Eq == null && fname.Any == null)
                    {
                        return idStorage.Except(_id2name.Keys);
                    }
                    else
                    {
                        return Enumerable.Empty<int>();
                    }
                }
            }

            if (fname.Eq != null && fname.Any != null)
            {
                if (fname.Any.Contains(fname.Eq))
                {
                    return _id2name.Where(x => x.Value == fname.Eq).Select(x => x.Key);
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (fname.Any != null)
            {
                HashSet<string> names = new HashSet<string>(fname.Any);
                return _id2name.Where(x => names.Contains(x.Value)).Select(x => x.Key);
            }
            else if (fname.Eq != null)
            {
                return _id2name.Where(x => x.Value == fname.Eq).Select(x => x.Key);
            }

            return _id2name.Keys;
        }
    }
}
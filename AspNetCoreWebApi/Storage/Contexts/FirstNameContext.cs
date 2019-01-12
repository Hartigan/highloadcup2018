using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class FirstNameContext : IBatchLoader<string>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private string[] _names = new string[DataConfig.MaxId];
        private HashSet<int> _ids = new HashSet<int>();

        public FirstNameContext()
        {
        }

        public void LoadBatch(IEnumerable<BatchEntry<string>> batch)
        {
            _rw.AcquireWriterLock(2000);
            foreach(var entry in batch)
            {
                _names[entry.Id] = string.Intern(entry.Value);
                _ids.Add(entry.Id);
            }
            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, string name)
        {
            _rw.AcquireWriterLock(2000);

            _names[id] = string.Intern(name);
            _ids.Add(id);

            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out string fname)
        {
            fname = _names[id];
            return fname != null;
        }

        public IEnumerable<int> Filter(
            FilterRequest.FnameRequest fname,
            IdStorage idStorage)
        {
            if (fname.IsNull != null)
            {
                if (fname.IsNull.Value)
                {
                    if (fname.Eq == null && fname.Any.Count == 0)
                    {
                        return idStorage.Except(_ids);
                    }
                    else
                    {
                        return Enumerable.Empty<int>();
                    }
                }
            }

            if (fname.Eq != null && fname.Any.Count > 0)
            {
                if (fname.Any.Contains(fname.Eq))
                {
                    return _ids.Where(x => _names[x] == fname.Eq);
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (fname.Any.Count > 0)
            {
                return _ids.Where(x => fname.Any.Contains(_names[x]));
            }
            else if (fname.Eq != null)
            {
                return _ids.Where(x => _names[x] == fname.Eq);
            }

            return _ids;
        }

        public void Compress()
        {
        }
    }
}
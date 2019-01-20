using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class LastNameContext : IBatchLoader<string>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private string[] _names = new string[DataConfig.MaxId];
        private List<int> _ids = new List<int>();
        private List<int> _null = new List<int>();

        public LastNameContext()
        {
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach(var id in ids.AsEnumerable())
            {
                if (_names[id] == null)
                {
                    _null.Add(id);
                }
            }
            _null.Sort(ReverseComparer<int>.Default);
            _null.TrimExcess();
        }

        public void LoadBatch(IEnumerable<BatchEntry<string>> batch)
        {
            _rw.AcquireWriterLock(2000);
            
            foreach(var entry in batch)
            {
                _names[entry.Id] = string.Intern(entry.Value);
                _ids.SortedInsert(entry.Id);
            }

            _rw.ReleaseWriterLock();
        }

        public void AddOrUpdate(int id, string name)
        {
            _rw.AcquireWriterLock(2000);

            _names[id] = string.Intern(name);
            _ids.SortedInsert(id);

            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out string sname)
        {
            sname = _names[id];
            return sname != null;
        }

        public IEnumerable<int> Filter(FilterRequest.SnameRequest sname, IdStorage idStorage)
        {
            if (sname.IsNull != null)
            {
                if (sname.IsNull.Value)
                {
                    if (sname.Eq == null && sname.Starts == null)
                    {
                        return _null;
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
                    return _ids.Where(x => _names[x] == sname.Eq);
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (sname.Starts != null)
            {
                return _ids.Where(x => _names[x].StartsWith(sname.Starts));
            }
            else if (sname.Eq != null)
            {
                return _ids.Where(x => _names[x] == sname.Eq);
            }

            return _ids;
        }

        public void Compress()
        {
            _ids.TrimExcess();
            _null.TrimExcess();
        }
    }
}
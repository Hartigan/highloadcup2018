using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class FirstNameContext : IBatchLoader<string>, ICompresable
    {
        private string[] _names = new string[DataConfig.MaxId];
        private DelaySortedList _ids = new DelaySortedList();
        private DelaySortedList _null = new DelaySortedList();

        public FirstNameContext()
        {
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach(var id in ids.AsEnumerable())
            {
                if (_names[id] == null)
                {
                    _null.Load(id);
                }
            }
            _null.LoadEnded();
        }

        public void LoadBatch(int id, string name)
        {
            _names[id] = string.Intern(name);
            _ids.Load(id);
        }

        public void AddOrUpdate(int id, string name)
        {
            if (_names[id] == null)
            {
                _ids.DelayAdd(id);
            }
            _names[id] = string.Intern(name);
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
                        return _null.AsEnumerable();
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
                    return _ids.AsEnumerable().Where(x => _names[x] == fname.Eq);
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (fname.Any.Count > 0)
            {
                return _ids.AsEnumerable().Where(x => fname.Any.Contains(_names[x]));
            }
            else if (fname.Eq != null)
            {
                return _ids.AsEnumerable().Where(x => _names[x] == fname.Eq);
            }

            return _ids.AsEnumerable();
        }

        public void Compress()
        {
            _ids.Flush();
        }

        public void LoadEnded()
        {
            _ids.LoadEnded();
        }
    }
}
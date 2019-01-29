using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class FirstNameContext : IBatchLoader<string>, ICompresable
    {
        private short[] _names = new short[DataConfig.MaxId];
        private DelaySortedList<int> _ids = DelaySortedList<int>.CreateDefault();
        private DelaySortedList<int> _null = DelaySortedList<int>.CreateDefault();
        private DelaySortedList<int>[] _byName = new DelaySortedList<int>[200];
        private readonly NameStorage _storage;

        public FirstNameContext(
            MainStorage storage)
        {
            _storage = storage.Names;
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach(var id in ids.AsEnumerable())
            {
                if (_names[id] == 0)
                {
                    _null.Load(id);
                }
            }
            _null.LoadEnded();
        }

        public void LoadBatch(int id, string name)
        {
            short nameId = _storage.Get(name);

            _names[id] = nameId;

            if (_byName[nameId] == null)
            {
                _byName[nameId] = DelaySortedList<int>.CreateDefault();
            }

            _byName[nameId].Load(id);

            _ids.Load(id);
        }

        public void AddOrUpdate(int id, string name)
        {
            short nameId = _storage.Get(name);

            if (_names[id] == 0)
            {
                _ids.DelayAdd(id);
            }
            else
            {
                _byName[nameId].DelayRemove(id);
            }

            _names[id] = nameId;

            if (_byName[nameId] == null)
            {
                _byName[nameId] = DelaySortedList<int>.CreateDefault();
                _byName[nameId].Load(id);
            }
            else
            {
                _byName[nameId].DelayAdd(id);
            }
        }

        public bool TryGet(int id, out string fname)
        {
            short nameId = _names[id];

            if (nameId > 0)
            {
                fname = _storage.GetString(nameId);
            }
            else
            {
                fname = null;
            }

            return nameId > 0;
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
                        return _null;
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
                    int eqId = _storage.Get(fname.Eq);
                    return _byName[eqId] ?? Enumerable.Empty<int>();
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (fname.Any.Count > 0)
            {
                List<IEnumerator<int>> enumerators = new List<IEnumerator<int>>(fname.Any.Count);
                for(int i = 0; i < fname.Any.Count; i++)
                {
                    int nameId = _storage.Get(fname.Any[i]);
                    if (_byName[nameId] != null)
                    {
                        enumerators.Add(_byName[nameId].GetEnumerator());
                    }
                }

                return ListHelper.MergeSort(enumerators, ReverseComparer<int>.Default);
            }
            else if (fname.Eq != null)
            {
                int eqId = _storage.Get(fname.Eq);
                return _byName[eqId] ?? Enumerable.Empty<int>();
            }

            return _ids;
        }

        public void Compress()
        {
            _ids.Flush();
            for(int i = 0; i < _byName.Length; i++)
            {
                _byName[i]?.Flush();
            }
        }

        public void LoadEnded()
        {
            _ids.LoadEnded();
            for (int i = 0; i < _byName.Length; i++)
            {
                _byName[i]?.LoadEnded();
            }
        }
    }
}
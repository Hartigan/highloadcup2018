using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class LastNameContext : IBatchLoader<string>, ICompresable
    {
        private short[] _names = new short[DataConfig.MaxId];
        private Dictionary<short, DelaySortedList<int>> _byName = new Dictionary<short, DelaySortedList<int>>(2000);
        private DelaySortedList<int> _ids = DelaySortedList<int>.CreateDefault();
        private DelaySortedList<int> _null = DelaySortedList<int>.CreateDefault();

        private readonly LastNameStorage _storage;

        public LastNameContext(MainStorage storage)
        {
            _storage = storage.LastNames;
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

        public void LoadBatch(int id, string lastname)
        {
            short nameId = _storage.Get(lastname);

            _names[id] = nameId;

            DelaySortedList<int> nameGroup;
            if (!_byName.TryGetValue(nameId, out nameGroup))
            {
                nameGroup = DelaySortedList<int>.CreateDefault();
                _byName.Add(nameId, nameGroup);
            }

            nameGroup.Load(id);

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

            DelaySortedList<int> nameGroup;
            if (!_byName.TryGetValue(nameId, out nameGroup))
            {
                nameGroup = DelaySortedList<int>.CreateDefault();
                _byName.Add(nameId, nameGroup);
                nameGroup.Load(id);
            }
            else
            {
                nameGroup.DelayAdd(id);
            }
        }

        public bool TryGet(int id, out string sname)
        {
            short nameId = _names[id];

            if (nameId > 0)
            {
                sname = _storage.GetString(nameId);
            }
            else
            {
                sname = null;
            }

            return nameId > 0;
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
                    return _byName.GetValueOrDefault(_storage.Get(sname.Eq)) ?? Enumerable.Empty<int>();
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }

            if (sname.Starts != null)
            {
                List<IEnumerator<int>> enumerators = new List<IEnumerator<int>>();

                foreach(var nameId in _storage.StartWith(sname.Starts))
                {
                    var list = _byName.GetValueOrDefault(nameId);
                    if (list != null)
                    {
                        enumerators.Add(list.GetEnumerator());
                    }
                }

                return ListHelper.MergeSort(enumerators, ReverseComparer<int>.Default);
            }
            else if (sname.Eq != null)
            {
                return _byName.GetValueOrDefault(_storage.Get(sname.Eq)) ?? Enumerable.Empty<int>();
            }

            return _ids;
        }

        public void Compress()
        {
            _ids.Flush();

            foreach(var list in _byName.Values)
            {
                list.Flush();
            }
        }

        public void LoadEnded()
        {
            _ids.LoadEnded();

            foreach (var list in _byName.Values)
            {
                list.LoadEnded();
            }
        }
    }
}
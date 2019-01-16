using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class PhoneContext : IBatchLoader<Phone>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private Phone?[] _phones = new Phone?[DataConfig.MaxId];
        private HashSet<int> _null = new HashSet<int>();
        private Dictionary<short, List<int>> _code2ids = new Dictionary<short, List<int>>();

        public PhoneContext()
        {
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            _null.UnionWith(ids.AsEnumerable());
            _null.ExceptWith(_code2ids.Values.SelectMany(x => x));
            _null.TrimExcess();
        }

        public void LoadBatch(IEnumerable<BatchEntry<Phone>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach(var entry in batch)
            {
                Phone phone = entry.Value;

                _phones[entry.Id] = phone;

                if (_code2ids.ContainsKey(phone.Code))
                {
                    _code2ids[phone.Code].Add(entry.Id);
                }
                else
                {
                    _code2ids[phone.Code] = new List<int>() { entry.Id };
                }
            }

            foreach(var code in batch.Select(x => x.Value.Code).Distinct())
            {
                _code2ids[code].Sort();
            }

            _rw.ReleaseWriterLock();
        }

        public void Add(int id, Phone phone)
        {
            _rw.AcquireWriterLock(2000);

            _phones[id] = phone;

            if (_code2ids.ContainsKey(phone.Code))
            {
                var list = _code2ids[phone.Code];
                list.Insert(~list.BinarySearch(id), id);
            }
            else
            {
                _code2ids[phone.Code] = new List<int>() { id };
            }

            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Phone phone)
        {
            _rw.AcquireWriterLock(2000);

            var old = _phones[id];

            if (old.HasValue)
            {
                var list = _code2ids[old.Value.Code];
                list.RemoveAt(list.BinarySearch(id));
            }

            Add(id, phone);

            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out Phone phone)
        {
            if (_phones[id].HasValue)
            {
                phone = _phones[id].Value;
                return true;
            }
            else
            {
                phone = default(Phone);
                return false;
            }
        }

        public IEnumerable<int> Filter(FilterRequest.PhoneRequest phone, IdStorage idStorage)
        {
            if (phone.IsNull.HasValue)
            {
                if (phone.IsNull.Value)
                {
                    return phone.Code.HasValue ? Enumerable.Empty<int>() : _null;
                }
            }

            if (phone.Code.HasValue)
            {
                if (_code2ids.ContainsKey(phone.Code.Value))
                {
                    return _code2ids[phone.Code.Value];
                }
                else
                {
                    return Enumerable.Empty<int>();
                }
            }
            else
            {
                return _code2ids.SelectMany(x => x.Value);
            }
        }

        public void Compress()
        {
            _null.TrimExcess();
            foreach(var list in _code2ids.Values)
            {
                list.Compress();
            }
        }
    }
}
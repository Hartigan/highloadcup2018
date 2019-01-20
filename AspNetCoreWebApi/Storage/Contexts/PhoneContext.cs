using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class PhoneContext : IBatchLoader<Phone>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private Phone[] _phones = new Phone[DataConfig.MaxId];
        private FilterSet _ids = new FilterSet();
        private FilterSet _null = new FilterSet();
        private Dictionary<short, FilterSet> _code2ids = new Dictionary<short, FilterSet>();

        public PhoneContext()
        {
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach (var id in ids.AsEnumerable())
            {
                if (!_ids.Contains(id))
                {
                    _null.Add(id);
                }
            }
        }

        public void LoadBatch(IEnumerable<BatchEntry<Phone>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach(var entry in batch)
            {
                Phone phone = entry.Value;

                _phones[entry.Id] = phone;

                if (!_code2ids.ContainsKey(phone.Code))
                {
                    _code2ids[phone.Code] = new FilterSet();
                }
                _code2ids[phone.Code].Add(entry.Id);
                _ids.Add(entry.Id);
            }

            _rw.ReleaseWriterLock();
        }

        public void Add(int id, Phone phone)
        {
            _rw.AcquireWriterLock(2000);

            _ids.Add(id);
            _phones[id] = phone;

            if (!_code2ids.ContainsKey(phone.Code))
            {
                _code2ids[phone.Code] = new FilterSet();
            }
            _code2ids[phone.Code].Add(id);

            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Phone phone)
        {
            _rw.AcquireWriterLock(2000);

            var old = _phones[id];

            if (_ids.Contains(id))
            {
                _code2ids[old.Code].Remove(id);
            }

            Add(id, phone);

            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out Phone phone)
        {
            if (_ids.Contains(id))
            {
                phone = _phones[id];
                return true;
            }
            else
            {
                phone = default(Phone);
                return false;
            }
        }

        public FilterSet Filter(FilterRequest.PhoneRequest phone, IdStorage idStorage)
        {
            if (phone.IsNull.HasValue)
            {
                if (phone.IsNull.Value)
                {
                    return phone.Code.HasValue ? FilterSet.Empty : _null;
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
                    return FilterSet.Empty;
                }
            }
            else
            {
                return _ids;
            }
        }

        public void Compress()
        {
        }
    }
}
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
        private Phone[] _phones = new Phone[DataConfig.MaxId];
        private DelaySortedList<int> _ids = DelaySortedList<int>.CreateDefault();
        private DelaySortedList<int> _null = DelaySortedList<int>.CreateDefault();
        private Dictionary<short, DelaySortedList<int>> _code2ids = new Dictionary<short, DelaySortedList<int>>();

        public PhoneContext()
        {
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach (var id in ids.AsEnumerable())
            {
                if (!_phones[id].IsNotEmpty())
                {
                    _null.Load(id);
                }
            }
            _null.LoadEnded();
        }

        public void LoadBatch(int id, Phone phone)
        {
            _phones[id] = phone;

            if (!_code2ids.ContainsKey(phone.Code))
            {
                _code2ids[phone.Code] = DelaySortedList<int>.CreateDefault();
            }
            _code2ids[phone.Code].Load(id);
            _ids.Load(id);
        }

        public void Add(int id, Phone phone)
        {
            if (!_phones[id].IsNotEmpty())
            {
                _ids.DelayAdd(id);
            }
            
            _phones[id] = phone;

            if (!_code2ids.ContainsKey(phone.Code))
            {
                _code2ids[phone.Code] = DelaySortedList<int>.CreateDefault();
            }
            _code2ids[phone.Code].DelayAdd(id);
        }

        public void Update(int id, Phone phone)
        {
            var old = _phones[id];

            if (_phones[id].IsNotEmpty())
            {
                _code2ids[old.Code].DelayRemove(id);
            }

            Add(id, phone);
        }

        public bool TryGet(int id, out Phone phone)
        {
            if (_phones[id].IsNotEmpty())
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
                return _ids;
            }
        }

        public void Compress()
        {
            _ids.Flush();
            foreach(var list in _code2ids.Values)
            {
                list.Flush();
            }
        }

        public void LoadEnded()
        {
            _ids.LoadEnded();
            foreach (var list in _code2ids.Values)
            {
                list.LoadEnded();
            }
        }
    }
}
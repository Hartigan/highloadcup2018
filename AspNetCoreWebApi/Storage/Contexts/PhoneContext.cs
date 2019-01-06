using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class PhoneContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, Phone> _id2phone = new SortedDictionary<int, Phone>();
        private SortedDictionary<short, HashSet<int>> _code2ids = new SortedDictionary<short, HashSet<int>>();

        public PhoneContext()
        {
        }

        public void Add(int id, Phone phone)
        {
            _rw.AcquireWriterLock(2000);
            _id2phone.Add(id, phone);

            if (_code2ids.ContainsKey(phone.Code))
            {
                _code2ids[phone.Code].Add(id);
            }
            else
            {
                _code2ids[phone.Code] = new HashSet<int>() { id };
            }

            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Phone phone)
        {
            _rw.AcquireWriterLock(2000);
            Phone old;
            if (_id2phone.TryGetValue(id, out old))
            {
                _code2ids[old.Code].Remove(id);
            }

            _id2phone[id] = phone;
            _code2ids[phone.Code].Add(id);
            _rw.ReleaseWriterLock();
        }

        public bool TryGet(int id, out Phone phone) => _id2phone.TryGetValue(id, out phone);

        public IEnumerable<int> Filter(FilterRequest.PhoneRequest phone, IdStorage idStorage)
        {
            if (phone.IsNull.HasValue)
            {
                if (phone.IsNull.Value)
                {
                    return phone.Code.HasValue ? Enumerable.Empty<int>() : idStorage.Except(_id2phone.Keys);
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
                return _id2phone.Keys;
            }
        }
    }
}
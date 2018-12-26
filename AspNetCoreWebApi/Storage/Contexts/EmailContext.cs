using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class EmailContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, Email> _id2email = new SortedDictionary<int, Email>();
        private Dictionary<Email, int> _email2id = new Dictionary<Email, int>();
        private SortedDictionary<int, HashSet<int>> _domain2ids = new SortedDictionary<int, HashSet<int>>();

        public EmailContext()
        {
        }

        public void Add(int id, Email email)
        {
            _rw.AcquireWriterLock(2000);
            email.Prefix = string.Intern(email.Prefix);
            _id2email.Add(id, email);
            _email2id.Add(email, id);

            if (_domain2ids.ContainsKey(email.DomainId))
            {
                _domain2ids[email.DomainId].Add(id);
            }
            else
            {
                _domain2ids[email.DomainId] = new HashSet<int>() { id };
            }

            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Email updated)
        {
            _rw.AcquireWriterLock(2000);
            updated.Prefix = string.Intern(updated.Prefix);
            var old = _id2email[id];
            _id2email[id] = updated;
            _email2id.Remove(old);
            _email2id.Add(updated, id);

            _domain2ids[old.DomainId].Remove(id);

            if (_domain2ids.ContainsKey(updated.DomainId))
            {
                _domain2ids[updated.DomainId].Add(id);
            }
            else
            {
                _domain2ids[updated.DomainId] = new HashSet<int>() { id };
            }

            _rw.ReleaseWriterLock();
        }

        public Email Get(int id)
        {
            _rw.AcquireReaderLock(2000);
            Email email = _id2email[id];
            _rw.ReleaseReaderLock();
            return email;
        }

        public IEnumerable<int> Filter(
            FilterRequest.EmailRequest email,
            DomainStorage domainStorage)
        {
            HashSet<int> withDomain = null;
            if (email.Domain != null)
            {
                var domainId = domainStorage.Get(email.Domain);
                withDomain = _domain2ids.GetValueOrDefault(domainId);
            }

            IEnumerable<int> result = withDomain != null ? (IEnumerable<int>)withDomain : _id2email.Keys;

            if (email.Gt != null && email.Lt != null)
            {
                if (string.Compare(email.Gt, email.Lt) > 0)
                {
                    return Enumerable.Empty<int>();
                }

                return result.Where(x =>
                {
                    string prefix = _id2email[x].Prefix;
                    return string.Compare(prefix, email.Gt) > 0 &&
                        string.Compare(prefix, email.Lt) < 0;
                });
            }

            if (email.Gt != null)
            {
                return result.Where(x => string.Compare(_id2email[x].Prefix, email.Gt) > 0);
            }
            else
            {
                return result.Where(x => string.Compare(_id2email[x].Prefix, email.Lt) < 0);
            }
        }
    }
}
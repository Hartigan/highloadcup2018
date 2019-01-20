using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class EmailContext : IBatchLoader<Email>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private Email[] _emails = new Email[DataConfig.MaxId];
        private Dictionary<short, List<int>> _domain2ids = new Dictionary<short, List<int>>();

        public EmailContext()
        {
        }

        public void Add(int id, Email email)
        {
            _rw.AcquireWriterLock(2000);
            email.Prefix = string.Intern(email.Prefix);
            _emails[id] = email;

            if (_domain2ids.ContainsKey(email.DomainId))
            {
                var list = _domain2ids[email.DomainId];
                list.SortedInsert(id);
            }
            else
            {
                _domain2ids[email.DomainId] = new List<int>() { id };
            }

            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Email updated)
        {
            _rw.AcquireWriterLock(2000);
            
            var old = _emails[id];
            var list = _domain2ids[old.DomainId];
            list.RemoveAt(list.BinarySearch(id, ReverseComparer<int>.Default));

            Add(id, updated);

            _rw.ReleaseWriterLock();
        }

        public Email Get(int id)
        {
            return _emails[id];
        }

        public IEnumerable<int> Filter(
            FilterRequest.EmailRequest email,
            DomainStorage domainStorage,
            IdStorage idStorage)
        {
            List<int> withDomain = null;
            if (email.Domain != null)
            {
                var domainId = domainStorage.Get(email.Domain);
                withDomain = _domain2ids.GetValueOrDefault(domainId);
            }

            IEnumerable<int> result = withDomain != null ? (IEnumerable<int>)withDomain : idStorage.AsEnumerable();

            if (email.Gt != null && email.Lt != null)
            {
                if (string.Compare(email.Gt, email.Lt) > 0)
                {
                    return Enumerable.Empty<int>();
                }

                return result.Where(x =>
                {
                    string prefix = _emails[x].Prefix;
                    return string.Compare(prefix, email.Gt) > 0 &&
                        string.Compare(prefix, email.Lt) < 0;
                });
            }

            if (email.Gt != null)
            {
                return result.Where(x => string.Compare(_emails[x].Prefix, email.Gt) > 0);
            }
            else if (email.Lt != null)
            {
                return result.Where(x => string.Compare(_emails[x].Prefix, email.Lt) < 0);
            }

            return result;
        }

        public void LoadBatch(IEnumerable<BatchEntry<Email>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach(var entry in batch)
            {
                Email email = entry.Value;
                int id = entry.Id;
                email.Prefix = string.Intern(email.Prefix);

                _emails[id] = email;

                if (_domain2ids.ContainsKey(email.DomainId))
                {
                    _domain2ids[email.DomainId].Add(id);
                }
                else
                {
                    _domain2ids[email.DomainId] = new List<int>() { id };
                }
            }

            foreach(var domainId in batch.Select(x => x.Value.DomainId).Distinct())
            {
                _domain2ids[domainId].Sort(ReverseComparer<int>.Default);
            }

            _rw.ReleaseWriterLock();
        }

        public void Compress()
        {
            foreach(var list in _domain2ids.Values)
            {
                list.Compress();
            }
        }
    }
}
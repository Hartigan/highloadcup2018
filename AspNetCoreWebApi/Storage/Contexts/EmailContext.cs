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
        private Email[] _emails = new Email[DataConfig.MaxId];
        private Dictionary<short, DelaySortedList<int>> _domain2ids = new Dictionary<short, DelaySortedList<int>>();

        public EmailContext()
        {
        }

        public void Add(int id, Email email)
        {
            email.Prefix = string.Intern(email.Prefix);
            _emails[id] = email;

            if (!_domain2ids.ContainsKey(email.DomainId))
            {
                _domain2ids[email.DomainId] = DelaySortedList<int>.CreateDefault();
            }

            _domain2ids[email.DomainId].DelayAdd(id);
        }

        public void Update(int id, Email updated)
        {
            var old = _emails[id];
            _domain2ids[old.DomainId].DelayRemove(id);
            Add(id, updated);
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
            DelaySortedList<int> withDomain = null;
            if (email.Domain != null)
            {
                var domainId = domainStorage.Get(email.Domain);
                withDomain = _domain2ids.GetValueOrDefault(domainId);
            }

            IEnumerable<int> result = withDomain != null ? withDomain : idStorage.AsEnumerable();

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

        public void LoadBatch(int id, Email email)
        {
            email.Prefix = string.Intern(email.Prefix);

            _emails[id] = email;

            if (!_domain2ids.ContainsKey(email.DomainId))
            {
                _domain2ids[email.DomainId] = DelaySortedList<int>.CreateDefault();
            }
            _domain2ids[email.DomainId].Load(id);
        }

        public void Compress()
        {
            foreach(var list in _domain2ids.Values)
            {
                list.Flush();
            }
        }

        public void LoadEnded()
        {
            foreach(var list in _domain2ids.Values)
            {
                list.LoadEnded();
            }
        }
    }
}
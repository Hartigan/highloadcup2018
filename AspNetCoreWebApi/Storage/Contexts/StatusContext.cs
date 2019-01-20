using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class StatusContext : IBatchLoader<Status>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private Dictionary<Status, FilterSet> _raw = new Dictionary<Status, FilterSet>();

        public StatusContext()
        {
            _raw[Status.Complicated] = new FilterSet();
            _raw[Status.Free] = new FilterSet();
            _raw[Status.Reserved] = new FilterSet();
        }

        public void LoadBatch(IEnumerable<BatchEntry<Status>> batch)
        {
            _rw.AcquireWriterLock(2000);
            foreach(var entry in batch)
            {
                _raw[entry.Value].Add(entry.Id);
            }
            _rw.ReleaseWriterLock();
        }

        public void Add(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);
            _raw[status].Add(id);
            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);

            foreach(var bitarray in _raw.Values)
            {
                bitarray.Remove(id);
            }

            _raw[status].Add(id);

            _rw.ReleaseWriterLock();
        }

        public Status Get(int id)
        {
            foreach (var pair in _raw)
            {
                if (pair.Value.Contains(id))
                {
                    return pair.Key;
                }
            }

            throw new ArgumentException("public Status Get(int id)");
        }

        public bool Filter(FilterRequest.StatusRequest status, FilterSet result)
        {
            if (status.Eq == status.Neq)
            {
                return false;
            }

            if (status.Eq != null)
            {
                result.IntersectWith(_raw[status.Eq.Value]);
                return true;
            }

            foreach(var pair in _raw.Where(x => x.Key != status.Neq.Value))
            {
                result.Add(pair.Value);
            }
            return true;
        }

        public FilterSet Filter(GroupRequest.StatusRequest status)
        {
            return _raw[status.Status];
        }

        public void FillGroups(List<Group> groups)
        {
            if (groups.Count == 0)
            {
                groups.Add(new Group(status: Status.Complicated));
                groups.Add(new Group(status: Status.Free));
                groups.Add(new Group(status: Status.Reserved));
            }
            else
            {
                int size = groups.Count;
                for (int i = 0; i < size; i++)
                {
                    Group g = groups[i];
                    g.Status = Status.Complicated;
                    groups[i] = g;
                    g.Status = Status.Free;
                    groups.Add(g);
                    g.Status = Status.Reserved;
                    groups.Add(g);
                }
            }
        }

        public bool Contains(Status status, int id)
        {
            return _raw[status].Contains(id);
        }

        public void Compress()
        {
        }
    }
}
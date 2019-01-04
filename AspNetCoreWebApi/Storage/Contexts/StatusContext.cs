using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class StatusContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private Dictionary<Status, HashSet<int>> _id2AccId = new Dictionary<Status, HashSet<int>>();

        public StatusContext()
        {
            _id2AccId[Status.Complicated] = new HashSet<int>();
            _id2AccId[Status.Free] = new HashSet<int>();
            _id2AccId[Status.Reserved] = new HashSet<int>();
        }

        public void Add(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);
            _id2AccId[status].Add(id);
            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);
            _id2AccId[Status.Complicated].Remove(id);
            _id2AccId[Status.Free].Remove(id);
            _id2AccId[Status.Reserved].Remove(id);
            _id2AccId[status].Add(id);
            _rw.ReleaseWriterLock();
        }

        public Status Get(int id)
        {
            _rw.AcquireReaderLock(2000);
            
            Status status = Status.Complicated;
            if (_id2AccId[Status.Free].Contains(id))
            {
                status = Status.Free;
            }

            if (_id2AccId[Status.Reserved].Contains(id))
            {
                status = Status.Reserved;
            }


            _rw.ReleaseReaderLock();
            return status;
        }

        public IEnumerable<int> Filter(FilterRequest.StatusRequest status)
        {
            if (status.Eq == status.Neq)
            {
                return Enumerable.Empty<int>();
            }

            if (status.Eq != null)
            {
                return _id2AccId[status.Eq.Value];
            }

            return _id2AccId.Where(x => x.Key != status.Neq.Value).SelectMany(x => x.Value);
        }

        public IEnumerable<int> Filter(GroupRequest.StatusRequest status)
        {
            return _id2AccId[status.Status];
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
                    Group g1 = groups[i].Copy();
                    Group g2 = groups[i].Copy();
                    groups[i].Status = Status.Complicated;
                    g1.Status = Status.Free;
                    g2.Status = Status.Reserved;
                    groups.Add(g1);
                    groups.Add(g2);
                }
            }
        }

        public bool Contains(Status status, int id)
        {
            return _id2AccId[status].Contains(id);
        }

        public void GetByStatus(Status value, HashSet<int> currentIds)
        {
            currentIds.UnionWith(_id2AccId[value]);
        }
    }
}
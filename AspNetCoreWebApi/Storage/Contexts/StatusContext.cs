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
        private FilterSet[] _raw = new FilterSet[3];

        public StatusContext()
        {
            _raw[(int)Status.Complicated] = new FilterSet();
            _raw[(int)Status.Free] = new FilterSet();
            _raw[(int)Status.Reserved] = new FilterSet();
        }

        public void LoadBatch(int id, Status status)
        {
            _raw[(int)status].Add(id);
        }

        public void Add(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);
            _raw[(int)status].Add(id);
            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);

            for(int i = 0; i < 3; i++)
            {
                _raw[i].Remove(id);
            }

            _raw[(int)status].Add(id);

            _rw.ReleaseWriterLock();
        }

        public Status Get(int id)
        {
            if (_raw[(int)Status.Free].Contains(id))
            {
                return Status.Free;
            }
            else if (_raw[(int)Status.Complicated].Contains(id))
            {
                return Status.Complicated;
            }
            else
            {
                return Status.Reserved;
            }
        }

        public bool Filter(FilterRequest.StatusRequest status, FilterSet result)
        {
            if (status.Eq == status.Neq)
            {
                return false;
            }

            if (status.Eq != null)
            {
                result.Add(_raw[(int)status.Eq.Value]);
                return true;
            }

            for(int i = 0; i < 3; i++)
            {
                if (i == (int)status.Neq)
                {
                    continue;
                }
                result.Add(_raw[i]);
            }
            return true;
        }

        public FilterSet Filter(GroupRequest.StatusRequest status)
        {
            return _raw[(int)status.Status];
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
            return _raw[(int)status].Contains(id);
        }

        public void Compress()
        {
        }
    }
}
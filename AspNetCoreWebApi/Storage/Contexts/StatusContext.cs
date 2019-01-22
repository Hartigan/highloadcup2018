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
        private CountSet[] _raw = new CountSet[3];

        public StatusContext()
        {
            _raw[(int)Status.Complicated] = new CountSet();
            _raw[(int)Status.Free] = new CountSet();
            _raw[(int)Status.Reserved] = new CountSet();
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

        public IFilterSet Filter(GroupRequest.StatusRequest status)
        {
            return _raw[(int)status.Status];
        }

        public bool Contains(Status status, int id)
        {
            return _raw[(int)status].Contains(id);
        }

        public IEnumerable<SingleKeyGroup<Status>> GetGroups()
        {
            yield return new SingleKeyGroup<Status>(Status.Complicated, _raw[(int)Status.Complicated].AsEnumerable(), _raw[(int)Status.Complicated].Count);
            yield return new SingleKeyGroup<Status>(Status.Free, _raw[(int)Status.Free].AsEnumerable(), _raw[(int)Status.Free].Count);
            yield return new SingleKeyGroup<Status>(Status.Reserved, _raw[(int)Status.Reserved].AsEnumerable(), _raw[(int)Status.Reserved].Count);
        }

        public void Compress()
        {
        }
    }
}
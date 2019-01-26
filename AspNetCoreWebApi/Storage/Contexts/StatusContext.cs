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
        private DelaySortedList[] _groups = new DelaySortedList[3];

        public StatusContext()
        {
            _raw[(int)Status.Complicated] = new CountSet();
            _raw[(int)Status.Free] = new CountSet();
            _raw[(int)Status.Reserved] = new CountSet();

            _groups[(int)Status.Complicated] = new DelaySortedList();
            _groups[(int)Status.Free] = new DelaySortedList();
            _groups[(int)Status.Reserved] = new DelaySortedList();
        }

        public void LoadBatch(int id, Status status)
        {
            _raw[(int)status].Add(id);
            _groups[(int)status].Load(id);
        }

        public void Add(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);
            _raw[(int)status].Add(id);
            _groups[(int)status].DelayAdd(id);
            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);

            for(int i = 0; i < 3; i++)
            {
                _raw[i].Remove(id);
                if (_groups[i].DelayRemove(id))
                {
                    break;
                }
            }

            _raw[(int)status].Add(id);
            _groups[(int)status].DelayAdd(id);

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

        public IEnumerable<int> Filter(FilterRequest.StatusRequest status)
        {
            if (status.Eq == status.Neq)
            {
                return Enumerable.Empty<int>();
            }

            if (status.Eq != null)
            {
                return _groups[(int)status.Eq.Value].AsEnumerable();
            }

            List<IEnumerator<int>> enumerators = new List<IEnumerator<int>>(2);
            for(int i = 0; i < 3; i++)
            {
                if (i == (int)status.Neq)
                {
                    continue;
                }
                enumerators.Add(_groups[i].AsEnumerable().GetEnumerator());
            }
            return ListHelper.MergeSort(enumerators);
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
            yield return new SingleKeyGroup<Status>(Status.Complicated, _groups[(int)Status.Complicated].AsEnumerable(), _groups[(int)Status.Complicated].Count);
            yield return new SingleKeyGroup<Status>(Status.Free, _groups[(int)Status.Free].AsEnumerable(), _groups[(int)Status.Free].Count);
            yield return new SingleKeyGroup<Status>(Status.Reserved, _groups[(int)Status.Reserved].AsEnumerable(), _groups[(int)Status.Reserved].Count);
        }

        public void Compress()
        {
            _rw.AcquireWriterLock(2000);
            _groups[0].Flush();
            _groups[1].Flush();
            _groups[2].Flush();
            _rw.ReleaseWriterLock();
        }

        public void LoadEnded()
        {
            _groups[0].LoadEnded();
            _groups[1].LoadEnded();
            _groups[2].LoadEnded();
        }
    }
}
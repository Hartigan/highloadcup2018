using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class StatusContext : IBatchLoader<Status>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private Dictionary<Status, BitArray> _raw = new Dictionary<Status, BitArray>();
        private Dictionary<Status, List<int>> _id2AccId = new Dictionary<Status, List<int>>();

        public StatusContext()
        {
            _id2AccId[Status.Complicated] = new List<int>();
            _id2AccId[Status.Free] = new List<int>();
            _id2AccId[Status.Reserved] = new List<int>();
            _raw[Status.Complicated] = new BitArray(DataConfig.MaxId);
            _raw[Status.Free] = new BitArray(DataConfig.MaxId);
            _raw[Status.Reserved] = new BitArray(DataConfig.MaxId);
        }

        public void LoadBatch(IEnumerable<BatchEntry<Status>> batch)
        {
            _rw.AcquireWriterLock(2000);
            _id2AccId[Status.Complicated].AddRange(batch.Where(x => x.Value == Status.Complicated).Select(x => x.Id));
            _id2AccId[Status.Free].AddRange(batch.Where(x => x.Value == Status.Free).Select(x => x.Id));
            _id2AccId[Status.Reserved].AddRange(batch.Where(x => x.Value == Status.Reserved).Select(x => x.Id));
            _id2AccId[Status.Complicated].Sort();
            _id2AccId[Status.Free].Sort();
            _id2AccId[Status.Reserved].Sort();
            foreach(var entry in batch)
            {
                _raw[entry.Value][entry.Id] = true;
            }
            _rw.ReleaseWriterLock();
        }

        public void Add(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);
            _raw[status][id] = true;
            var list = _id2AccId[status];
            list.Insert(~list.BinarySearch(id), id);
            _rw.ReleaseWriterLock();
        }

        public void Update(int id, Status status)
        {
            _rw.AcquireWriterLock(2000);

            foreach(var bitarray in _raw.Values)
            {
                bitarray[id] = false;
            }

            _raw[status][id] = true;
            var list = _id2AccId[status];

            if (list.BinarySearch(id) < 0)
            {
                list.Insert(~list.BinarySearch(id), id);

                foreach(var l in _id2AccId)
                {
                    if (l.Key == status)
                    {
                        continue;
                    }

                    int index = l.Value.BinarySearch(id);
                    if (index >= 0)
                    {
                        l.Value.RemoveAt(index);
                        break;
                    }
                }
            }
            _rw.ReleaseWriterLock();
        }

        public Status Get(int id)
        {
            foreach (var pair in _raw)
            {
                if (pair.Value[id])
                {
                    return pair.Key;
                }
            }

            throw new ArgumentException("public Status Get(int id)");
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
            return _raw[status][id];
        }

        public void GetByStatus(Status value, HashSet<int> currentIds)
        {
            currentIds.UnionWith(_id2AccId[value]);
        }

        public void Compress()
        {
            foreach(var list in _id2AccId.Values)
            {
                list.Compress();
            }
        }
    }
}
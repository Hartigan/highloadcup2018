using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class InterestsContext : IBatchLoader<IEnumerable<short>>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private Dictionary<short, List<int>> _id2AccId = new Dictionary<short, List<int>>();
        private HashSet<int> _null = new HashSet<int>();

        public InterestsContext()
        {
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            _null.UnionWith(ids.AsEnumerable());
            _null.ExceptWith(_id2AccId.Values.SelectMany(x => x));
            _null.TrimExcess();
        }

        public void Add(int id, short interestId)
        {
            _rw.AcquireWriterLock(2000);
            if (_id2AccId.ContainsKey(interestId))
            {
                var list = _id2AccId[interestId];
                list.Insert(~list.BinarySearch(id), id);
            }
            else
            {
                _id2AccId[interestId] = new List<int>() { id };
            }
            _rw.ReleaseWriterLock();
        }

        public void RemoveAccount(int id)
        {
            _rw.AcquireWriterLock(2000);
            foreach(var list in _id2AccId.Values)
            {
                int index = list.BinarySearch(id);
                if (index >= 0)
                {
                    list.RemoveAt(index);
                }
            }
            _rw.ReleaseWriterLock();
        }

        public IEnumerable<int> Filter(
            FilterRequest.InterestsRequest interests,
            InterestStorage interestsStorage)
        {
            if (interests.Contains.Count > 0)
            {
                var ids = interests.Contains.Select(x => interestsStorage.Get(x));
                HashSet<int> result = null;

                foreach(var interest in ids)
                {
                    var tmp = _id2AccId.GetValueOrDefault(interest);
                    if (tmp == null)
                    {
                        return Enumerable.Empty<int>();
                    }

                    if (result == null)
                    {
                        result = new HashSet<int>(tmp);
                    }
                    else
                    {
                        result.IntersectWith(tmp);
                    }
                }

                return result ?? Enumerable.Empty<int>();
            }

            if (interests.Any.Count > 0)
            {
                var ids = interests.Any.Select(x => interestsStorage.Get(x));
                HashSet<int> result = null;

                foreach(var interest in ids)
                {
                    var tmp = _id2AccId.GetValueOrDefault(interest);
                    if (tmp == null)
                    {
                        continue;
                    }

                    if (result == null)
                    {
                        result = new HashSet<int>(tmp);
                    }
                    else
                    {
                        result.UnionWith(tmp);
                    }
                }

                return result ?? Enumerable.Empty<int>();
            }

            return Enumerable.Empty<int>();
        }

        public IEnumerable<int> Filter(
            GroupRequest.InterestRequest interests,
            InterestStorage interestsStorage)
        {
            short id = interestsStorage.Get(interests.Interest);
            return _id2AccId.GetValueOrDefault(id) ?? Enumerable.Empty<int>();
        }

        public void FillGroups(List<short?> groups)
        {
            groups.AddRange(_id2AccId.Keys.Cast<short?>());
            groups.Add(null);
        }

        public bool Contains(short? interestId, int id)
        {
            if (interestId.HasValue)
            {
                List<int> ids;
                if (_id2AccId.TryGetValue(interestId.Value, out ids))
                {
                    return ids.BinarySearch(id) >= 0;
                }
                else
                {
                    return false;
                }
            }

            return _null.Contains(id);
        }

        public void Recommend(int id, Dictionary<int, int> recomended)
        {
            foreach(var pair in _id2AccId)
            {
                if (pair.Value.BinarySearch(id) >= 0)
                {
                    foreach(var acc in pair.Value)
                    {
                        if (recomended.ContainsKey(acc))
                        {
                            recomended[acc]++;
                        }
                        else
                        {
                            recomended[acc] = 1;
                        }
                    }
                }
            }

            recomended.Remove(id);
        }

        public IEnumerable<int> GetByInterestId(
            short? interestId)
        {
            if (interestId.HasValue)
            {
                return _id2AccId[interestId.Value];
            }
            else
            {
                return _null;
            }
        }

        public void LoadBatch(IEnumerable<BatchEntry<IEnumerable<short>>> batch)
        {
            _rw.AcquireWriterLock(2000);

            foreach(var entry in batch)
            {
                int id = entry.Id;

                foreach(var interestId in entry.Value)
                {
                    if (_id2AccId.ContainsKey(interestId))
                    {
                        _id2AccId[interestId].Add(id);
                    }
                    else
                    {
                        _id2AccId[interestId] = new List<int>() { id };
                    }
                }
            }

            foreach(var interestId in batch.SelectMany(x=> x.Value).Distinct())
            {
                _id2AccId[interestId].Sort();
            }

            _rw.ReleaseWriterLock();
        }

        public void Compress()
        {
            _null.TrimExcess();
            foreach(var list in _id2AccId.Values)
            {
                list.Compress();
            }
        }
    }
}
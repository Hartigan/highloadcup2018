using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class InterestsContext : IBatchLoader<IEnumerable<int>>
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, List<int>> _id2AccId = new SortedDictionary<int, List<int>>();

        public InterestsContext()
        {
        }

        public void Add(int id, int interestId)
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
            if (interests.Contains != null)
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

            if (interests.Any != null)
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
            int id = interestsStorage.Get(interests.Interest);
            return _id2AccId.GetValueOrDefault(id) ?? Enumerable.Empty<int>();
        }

        public void FillGroups(List<Group> groups)
        {
            if (groups.Count == 0)
            {
                groups.AddRange(_id2AccId.Keys.Select(x => new Group(interestId: x)));
            }
            else
            {
                int size = groups.Count;
                for (int i = 0; i < size; i++)
                {
                    foreach(var key in _id2AccId.Keys)
                    {
                        Group g = groups[i].Copy();
                        g.InterestId = key;
                        groups.Add(g);
                    }
                }
            }
        }

        public bool Contains(int? interestId, int id)
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

            foreach(var ids in _id2AccId.Values)
            {
                if (ids.BinarySearch(id) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        public void Recommend(int id, IDictionary<int, int> recomended)
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

        public void GetByInterestId(
            int? interestId,
            HashSet<int> currentIds,
            IdStorage ids)
        {
            if (interestId.HasValue)
            {
                currentIds.UnionWith(_id2AccId[interestId.Value]);
            }
            else
            {
                currentIds.UnionWith(ids.Except(_id2AccId.SelectMany(x => x.Value)));
            }
        }

        public void LoadBatch(IEnumerable<BatchEntry<IEnumerable<int>>> batch)
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
    }
}
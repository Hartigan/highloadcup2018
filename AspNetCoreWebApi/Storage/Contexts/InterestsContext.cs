using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class InterestsContext
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, HashSet<int>> _id2AccId = new SortedDictionary<int, HashSet<int>>();

        public InterestsContext()
        {
        }

        public void Add(int id, int interestId)
        {
            _rw.AcquireWriterLock(2000);
            if (_id2AccId.ContainsKey(interestId))
            {
                _id2AccId[interestId].Add(id);
            }
            else
            {
                _id2AccId[interestId] = new HashSet<int>() { id };
            }
            _rw.ReleaseWriterLock();
        }

        public void RemoveAccount(int id)
        {
            _rw.AcquireWriterLock(2000);
            foreach(var value in _id2AccId.Values)
            {
                value.Remove(id);
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
                groups.Add(new Group());
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
                return _id2AccId[interestId.Value].Contains(id);
            }

            return !_id2AccId.Values.SelectMany(x => x).Contains(id);
        }

        public void Recommend(int id, IDictionary<int, int> recomended)
        {
            foreach(var pair in _id2AccId)
            {
                if (pair.Value.Contains(id))
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
    }
}
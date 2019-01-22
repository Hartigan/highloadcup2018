using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Pooling;
using AspNetCoreWebApi.Processing.Requests;
using AspNetCoreWebApi.Storage.StringPools;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class InterestsContext : IBatchLoader<IEnumerable<short>>, ICompresable
    {
        private ReaderWriterLock _rw = new ReaderWriterLock();
        private FilterSet[] _id2AccId = new FilterSet[200];
        private List<int> _null = new List<int>();

        public InterestsContext()
        {
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach(var id in ids.AsEnumerable())
            {
                if (!_id2AccId.Any(x => x != null && x.Contains(id)))
                {
                    _null.Add(id);
                }
            }
            _null.Sort(ReverseComparer<int>.Default);
            _null.TrimExcess();
        }

        public IEnumerable<short> GetAccountInterests(int id)
        {
            for(short i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null && _id2AccId[i].Contains(id))
                {
                    yield return i;
                }
            }
        }

        public void Add(int id, short interestId)
        {
            _rw.AcquireWriterLock(2000);
            if (_id2AccId[interestId] == null)
            {
                _id2AccId[interestId] = new FilterSet();
            }
            _id2AccId[interestId].Add(id);
            _rw.ReleaseWriterLock();
        }

        public void RemoveAccount(int id)
        {
            _rw.AcquireWriterLock(2000);
            for(int i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null)
                {
                    _id2AccId[i].Remove(id);
                }
            }
            _rw.ReleaseWriterLock();
        }

        public void Filter(
            FilterRequest.InterestsRequest interests,
            InterestStorage interestsStorage,
            FilterSet output)
        {
            if (interests.Contains.Count > 0)
            {
                var ids = interests.Contains.Select(x => interestsStorage.Get(x));
                bool inited = false;
                foreach(var interest in ids)
                {
                    var tmp = _id2AccId[interest];
                    if (tmp == null)
                    {
                        return;
                    }

                    if (!inited)
                    {
                        output.Add(tmp);
                        inited = true;
                    }
                    else
                    {
                        output.IntersectWith(tmp);
                    }
                }

                return;
            }

            if (interests.Any.Count > 0)
            {
                var ids = interests.Any.Select(x => interestsStorage.Get(x));

                foreach(var interest in ids)
                {
                    var tmp = _id2AccId[interest];
                    if (tmp == null)
                    {
                        continue;
                    }

                    output.Add(tmp);
                }

                return;
            }
        }

        public FilterSet Filter(
            GroupRequest.InterestRequest interests,
            InterestStorage interestsStorage)
        {
            short id = interestsStorage.Get(interests.Interest);
            return _id2AccId[id] ?? FilterSet.Empty;
        }

        public void FillGroups(List<short?> groups)
        {
            for(short i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null)
                {
                    groups.Add(i);
                }
            }
            groups.Add(null);
        }

        public void Recommend(int id, Dictionary<int, int> recomended)
        {
            for(int i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] == null)
                {
                    continue;
                }

                if (_id2AccId[i].Contains(id))
                {
                    foreach(var acc in _id2AccId[i].AsEnumerable())
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
                return _id2AccId[interestId.Value].AsEnumerable();
            }
            else
            {
                return _null;
            }
        }

        public void LoadBatch(int id, IEnumerable<short> interestsIds)
        {
            foreach(var interestId in interestsIds)
            {
                if (_id2AccId[interestId] == null)
                {
                    _id2AccId[interestId] = new FilterSet();
                }
                _id2AccId[interestId].Add(id);
            }
        }

        public void Compress()
        {
            _null.TrimExcess();
        }
    }
}
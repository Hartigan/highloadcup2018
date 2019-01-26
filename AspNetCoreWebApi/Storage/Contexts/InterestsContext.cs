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
        private DelaySortedList[] _id2AccId = new DelaySortedList[200];
        private DelaySortedList _null = new DelaySortedList();
        private CountSet _ids = new CountSet();

        private readonly MainPool _pool;

        public InterestsContext(MainPool pool)
        {
            _pool = pool;
        }

        public void InitNull(IdStorage ids)
        {
            _null.Clear();
            foreach(var id in ids.AsEnumerable())
            {
                if (!_ids.Contains(id))
                {
                    _null.Load(id);
                }
            }
            _null.LoadEnded();
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
            _ids.Add(id);
            if (_id2AccId[interestId] == null)
            {
                _id2AccId[interestId] = new DelaySortedList();
            }
            _id2AccId[interestId].DelayAdd(id);
            _rw.ReleaseWriterLock();
        }

        public void RemoveAccount(int id)
        {
            _rw.AcquireWriterLock(2000);
            _ids.Remove(id);
            for(int i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null)
                {
                    _id2AccId[i].DelayRemove(id);
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
                        output.Add(tmp.AsEnumerable());
                        inited = true;
                    }
                    else
                    {
                        var filterSet = _pool.FilterSet.Get();
                        filterSet.Add(tmp.AsEnumerable());
                        output.IntersectWith(filterSet);
                        _pool.FilterSet.Return(filterSet);
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

                    output.Add(tmp.AsEnumerable());
                }

                return;
            }
        }

        public IEnumerable<int> Filter(
            GroupRequest.InterestRequest interests,
            InterestStorage interestsStorage)
        {
            short id = interestsStorage.Get(interests.Interest);
            return _id2AccId[id]?.AsEnumerable() ?? Enumerable.Empty<int>();
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
                return _null.AsEnumerable();
            }
        }

        public void LoadBatch(int id, IEnumerable<short> interestsIds)
        {
            foreach(var interestId in interestsIds)
            {
                if (_id2AccId[interestId] == null)
                {
                    _id2AccId[interestId] = new DelaySortedList();
                }
                _id2AccId[interestId].Load(id);
            }
        }

        public IEnumerable<SingleKeyGroup<short>> GetGroups()
        {
            for(short i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null && _id2AccId[i].Count > 0)
                {
                    yield return new SingleKeyGroup<short>(i, _id2AccId[i].AsEnumerable(), _id2AccId[i].Count);
                }
            }
        }

        public void Compress()
        {
            _rw.AcquireWriterLock(2000);
            for(short i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null)
                {
                    _id2AccId[i].Flush();
                }
            }
            _rw.ReleaseWriterLock();
        }

        public void LoadEnded()
        {
            for(short i = 0; i < _id2AccId.Length; i++)
            {
                if (_id2AccId[i] != null)
                {
                    _id2AccId[i].LoadEnded();
                }
            }
        }
    }
}
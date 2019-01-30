using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class LikesContext : IBatchLoader<Like>, ICompresable
    {
        private class BucketIdComparer : IComparer<LikeBucket>
        {
            public static IComparer<LikeBucket> Default { get; } = new BucketIdComparer();

            public int Compare(LikeBucket x, LikeBucket y)
            {
                return y.LikeeId - x.LikeeId;
            }
        }

        public struct LikeBucket
        {
            public LikeBucket(int likeeId, int tsSum, int count)
            {
                LikeeId = likeeId;
                TsSum = tsSum;
                Count = count;
            }

            public int LikeeId;
            public int TsSum;
            public int Count;

            public static LikeBucket operator+(LikeBucket l, LikeBucket r)
            {
                return new LikeBucket(l.LikeeId, l.TsSum + r.TsSum, l.Count + r.Count);
            }

            public float Calc()
            {
                return 1.0f * TsSum / Count;
            }
        }

        private DelaySortedList<int>[] _likee2likers = new DelaySortedList<int>[DataConfig.MaxId];
        private DelaySortedList<LikeBucket>[] _liker2likes = new DelaySortedList<LikeBucket>[DataConfig.MaxId];

        public LikesContext()
        {
        }

        public void Add(Like like)
        {
            AddImpl(like, false);
        }

        private void AddImpl(Like like, bool import)
        {
            if (_likee2likers[like.LikeeId] != null)
            {
                var list = _likee2likers[like.LikeeId];
                {
                    if (import)
                    {
                        var rawList = list.GetList();
                        int index = rawList.BinarySearch(like.LikerId, ReverseComparer<int>.Default);
                        if (index < 0)
                        {
                            list.Insert(~index, like.LikerId);
                        }
                    }
                    else
                    {
                        if (!list.FullContains(like.LikerId))
                        {
                            list.DelayAdd(like.LikerId);
                        }
                    }
                }
            }
            else
            {
                _likee2likers[like.LikeeId] = DelaySortedList<int>.CreateDefault();
                _likee2likers[like.LikeeId].Load(like.LikerId);
            }

            DelaySortedList<LikeBucket> likes;
            if (_liker2likes[like.LikerId] == null)
            {
                _liker2likes[like.LikerId] = new DelaySortedList<LikeBucket>(BucketIdComparer.Default);
            }

            likes = _liker2likes[like.LikerId];

            LikeBucket bucket = new LikeBucket(like.LikeeId, like.Timestamp.Seconds, 1);

            if (import)
            {
                var rawList = likes.GetList();
                int index = rawList.BinarySearch(bucket, BucketIdComparer.Default);
                if (index >= 0)
                {
                    rawList[index] += bucket;
                }
                else
                {
                    likes.Insert(~index, bucket);
                }
            }
            else
            {
                likes.UpdateOrAdd(bucket, x => x + bucket);
            }
        }

        public IEnumerable<IIterator<int>> Filter(FilterRequest.LikesRequest likes)
        {
            foreach(var likee in likes.Contains)
            {
                var tmp = _likee2likers[likee];
                if (tmp == null)
                {
                    yield return ListHelper.EmptyInt;
                    break;
                }
                else
                {
                    yield return tmp.GetIterator();
                }
            }
        }

        public IEnumerable<int> Filter(GroupRequest.LikeRequest like)
        {
            return _likee2likers[like.Id] ?? Enumerable.Empty<int>();
        }

        public void Suggest(
            int id,
            Dictionary<int, float> similarity,
            Dictionary<int, List<LikeBucket>> suggested,
            HashSet<int> selfIds,
            MainContext context,
            short cityId,
            short countryId)
        {
            bool curSex = context.Sex.Get(id);

            DelaySortedList<LikeBucket> buckets = _liker2likes[id];
            if (buckets == null)
            {
                return;
            }

            foreach(var likeePair in buckets)
            {
                var likers = _likee2likers[likeePair.LikeeId];
                if (likers == null)
                {
                    continue;
                }

                foreach (var liker in likers)
                {
                    if (curSex != context.Sex.Get(liker))
                    {
                        continue;
                    }

                    if (cityId > 0 && cityId != context.Cities.Get(liker))
                    {
                        continue;
                    }

                    if (countryId > 0 && countryId != context.Countries.Get(liker))
                    {
                        continue;
                    }

                    float current = 0;
                    if (!similarity.TryGetValue(liker, out current))
                    {
                        current = 0;
                    }

                    float x = likeePair.Calc();
                    LikeBucket bucketY = new LikeBucket(likeePair.LikeeId, 0, 0);
                    var likerList = _liker2likes[liker];
                    bucketY = likerList.Find(bucketY);
                    float y = bucketY.Calc();

                    if (likeePair.TsSum * bucketY.Count == bucketY.TsSum * likeePair.Count)
                    {
                        current += 1.0f;
                    }
                    else
                    {
                        current += 1.0f / Math.Abs(x - y);
                    }
                    similarity[liker] = current;
                }
            }

            for(int i = 0; i < buckets.Count; i++)
            {
                selfIds.Add(buckets[i].LikeeId);
            }

            foreach(var liker in similarity.Keys)
            {
                suggested.Add(liker, _liker2likes[liker].GetList());
            }
        }

        public void LoadBatch(int id, Like like)
        {
            AddImpl(like, true);
        }

        public void Compress()
        {
            foreach(var list in _likee2likers)
            {
                if (list != null)
                {
                    list.Flush();
                }
            }

            foreach (var list in _liker2likes)
            {
                if (list != null)
                {
                    list.Flush();
                }
            }
        }

        public void LoadEnded()
        {
        }
    }
}
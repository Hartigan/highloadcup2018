using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class LikesContext : IBatchLoader<IEnumerable<Like>>, ICompresable
    {
        private class BucketIdComparer : IComparer<LikeBucket>
        {
            public int Compare(LikeBucket x, LikeBucket y)
            {
                return y.LikeeId - x.LikeeId;
            }
        }

        private struct LikeBucket
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

        private static BucketIdComparer _bucketKeyComparer = new BucketIdComparer();
        private List<int>[] _likee2likers = new List<int>[DataConfig.MaxId];
        private List<LikeBucket>[] _liker2likes = new List<LikeBucket>[DataConfig.MaxId];

        public LikesContext()
        {
        }

        public void Add(Like like)
        {
            AddImpl(like);
        }

        private void AddImpl(Like like)
        {
            if (_likee2likers[like.LikeeId] != null)
            {
                var list = _likee2likers[like.LikeeId];
                int likerIndex = list.BinarySearch(like.LikerId, ReverseComparer<int>.Default);
                if (likerIndex < 0)
                {
                    list.Insert(~likerIndex, like.LikerId);
                }
            }
            else
            {
                _likee2likers[like.LikeeId] = new List<int>() { like.LikerId };
            }

            List<LikeBucket> likes;
            if (_liker2likes[like.LikerId] == null)
            {
                _liker2likes[like.LikerId] = new List<LikeBucket>();
            }

            likes = _liker2likes[like.LikerId];

            LikeBucket bucket = new LikeBucket(like.LikeeId, like.Timestamp.Seconds, 1);
            int index = likes.BinarySearch(bucket, _bucketKeyComparer);
            if (index >= 0)
            {
                likes[index] += bucket;
            }
            else
            {
                likes.Insert(~index, bucket);
            }
        }

        public IEnumerable<int> Filter(FilterRequest.LikesRequest likes)
        {
            HashSet<int> result = null;

            foreach(var likee in likes.Contains)
            {
                var tmp = _likee2likers[likee];
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

            return result ?? Enumerable.Empty<int>();;
        }

        public IEnumerable<int> Filter(GroupRequest.LikeRequest like)
        {
            return _likee2likers[like.Id] ?? Enumerable.Empty<int>();
        }

        public void Suggest(
            int id,
            Dictionary<int, float> similarity,
            Dictionary<int, IEnumerable<int>> suggested)
        {
            List<LikeBucket> buckets = _liker2likes[id];
            if (buckets == null)
            {
                return;
            }

            foreach(var likeePair in buckets)
            {
                List<int> likers = _likee2likers[likeePair.LikeeId];
                if (likers == null)
                {
                    continue;
                }

                foreach (var liker in likers)
                {
                    float current = 0;
                    if (!similarity.TryGetValue(liker, out current))
                    {
                        current = 0;
                    }

                    float x = likeePair.Calc();
                    LikeBucket bucketY = new LikeBucket(likeePair.LikeeId, 0, 0);
                    var likerList = _liker2likes[liker];
                    bucketY = likerList[likerList.BinarySearch(bucketY, _bucketKeyComparer)];
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

            HashSet<int> selfIds = new HashSet<int>(buckets.Select(x => x.LikeeId));

            foreach(var liker in similarity.Keys)
            {
                suggested.Add(liker, _liker2likes[liker].Where(x => !selfIds.Contains(x.LikeeId)).Select(x => x.LikeeId));
            }
        }

        public void LoadBatch(int id, IEnumerable<Like> likes)
        {
            foreach(var like in likes)
            {
                AddImpl(like);
            }
        }

        public void Compress()
        {
            foreach(var list in _likee2likers)
            {
                if (list != null)
                {
                    list.Compress();
                }
            }

            foreach (var list in _liker2likes)
            {
                if (list != null)
                {
                    list.Compress();
                }
            }
        }

        public void LoadEnded()
        {
        }
    }
}
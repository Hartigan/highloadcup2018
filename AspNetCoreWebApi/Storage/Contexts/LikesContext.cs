using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class LikesContext : IBatchLoader<IEnumerable<Like>>
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

            public double Calc()
            {
                return 1.0 * TsSum / Count;
            }
        }

        private static BucketIdComparer _bucketKeyComparer = new BucketIdComparer();

        private ReaderWriterLock _rw = new ReaderWriterLock();
        private List<int>[] _likee2likers = new List<int>[DataConfig.MaxId];
        private List<LikeBucket>[] _liker2likes = new List<LikeBucket>[DataConfig.MaxId];

        public LikesContext()
        {
        }

        public void Add(Like like)
        {
            _rw.AcquireWriterLock(2000);

            if (_likee2likers[like.LikeeId] != null)
            {
                var list = _likee2likers[like.LikeeId];
                int likerIndex = list.BinarySearch(like.LikerId);
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

            LikeBucket bucket = new LikeBucket(like.LikeeId, (int)like.Timestamp.ToUnixTimeSeconds(), 1);
            int index = likes.BinarySearch(bucket, _bucketKeyComparer);
            if (index >= 0)
            {
                likes[index] += bucket;
            }
            else
            {
                likes.Insert(~index, bucket);
            }

            _rw.ReleaseWriterLock();
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
            IDictionary<int, double> similarity,
            IDictionary<int, IEnumerable<int>> suggested)
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
                    double current = 0;
                    if (!similarity.TryGetValue(liker, out current))
                    {
                        current = 0;
                    }

                    double x = likeePair.Calc();
                    LikeBucket bucketY = new LikeBucket(likeePair.LikeeId, 0, 0);
                    var likerList = _liker2likes[liker];
                    bucketY = likerList[likerList.BinarySearch(bucketY, _bucketKeyComparer)];
                    double y = bucketY.Calc();

                    if (likeePair.TsSum * bucketY.Count == bucketY.TsSum * likeePair.Count)
                    {
                        current += 1.0;
                    }
                    else
                    {
                        current += 1.0 / Math.Abs(x - y);
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

        public void LoadBatch(IEnumerable<BatchEntry<IEnumerable<Like>>> batch)
        {
            foreach(var entry in batch)
            {
                foreach(var like in entry.Value)
                {
                    this.Add(like);
                }
            }
        }
    }
}
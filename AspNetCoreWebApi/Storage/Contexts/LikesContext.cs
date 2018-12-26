using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AspNetCoreWebApi.Domain;
using AspNetCoreWebApi.Processing.Requests;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class LikesContext
    {
        private struct LikeBucket
        {
            public int TsSum;
            public int Count;
        }

        private ReaderWriterLock _rw = new ReaderWriterLock();
        private SortedDictionary<int, HashSet<int>> _likee2likers = new SortedDictionary<int, HashSet<int>>();
        private SortedDictionary<int, SortedDictionary<int, LikeBucket>> _liker2likes = new SortedDictionary<int, SortedDictionary<int, LikeBucket>>();

        public LikesContext()
        {
        }

        public void Add(Like like)
        {
            _rw.AcquireWriterLock(2000);

            if (_likee2likers.ContainsKey(like.LikeeId))
            {
                _likee2likers[like.LikeeId].Add(like.LikerId);
            }
            else
            {
                _likee2likers[like.LikeeId] = new HashSet<int>() { like.LikerId };
            }

            SortedDictionary<int, LikeBucket> likes;
            if (!_liker2likes.TryGetValue(like.LikerId, out likes))
            {
                likes = new SortedDictionary<int, LikeBucket>();
                _liker2likes.Add(like.LikerId, likes);
            }

            LikeBucket bucket;
            if (likes.TryGetValue(like.LikeeId, out bucket))
            {
                bucket.Count++;
                bucket.TsSum += (int)like.Timestamp.ToUnixTimeSeconds();
            }
            else
            {
                bucket.Count = 1;
                bucket.TsSum = (int)like.Timestamp.ToUnixTimeSeconds();
            }

            likes[like.LikeeId] = bucket;

            _rw.ReleaseWriterLock();
        }

        public IEnumerable<int> Filter(FilterRequest.LikesRequest likes)
        {
            HashSet<int> result = null;

            foreach(var likee in likes.Contains)
            {
                var tmp = _likee2likers.GetValueOrDefault(likee);
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
            return _likee2likers.GetValueOrDefault(like.Id) ?? Enumerable.Empty<int>();
        }

        public void Suggest(
            int id,
            IDictionary<int, float> similarity,
            IDictionary<int, IEnumerable<int>> suggested)
        {
            SortedDictionary<int, LikeBucket> buckets;
            if (!_liker2likes.TryGetValue(id, out buckets))
            {
                return;
            }

            foreach(var likeePair in buckets)
            {
                HashSet<int> likers;
                if (!_likee2likers.TryGetValue(likeePair.Key, out likers))
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

                    int x = likeePair.Value.TsSum / likeePair.Value.Count;
                    LikeBucket bucketY = _liker2likes[liker][likeePair.Key];
                    int y = bucketY.TsSum / bucketY.Count;

                    if (x == y)
                    {
                        current += 1.0f;
                    }
                    else
                    {
                        current += 1.0f / Math.Abs(x - y);
                    }
                }

                foreach(var liker in similarity.Keys)
                {
                    suggested.Add(liker, _liker2likes[liker].Keys);
                }
            }
        }
    }
}
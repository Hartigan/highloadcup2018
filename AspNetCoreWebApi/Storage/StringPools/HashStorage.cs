using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace AspNetCoreWebApi.Storage.StringPools
{
    public struct Hash
    {
        public int H41;
        public int H43;
        public int H47;
    }

    public class HashStorage
    {
        private readonly ReaderWriterLock _rw = new ReaderWriterLock();
        private readonly Dictionary<Hash, int> _hash2id = new Dictionary<Hash, int>();
        private readonly Dictionary<int, Hash> _id2hash = new Dictionary<int, Hash>();

        public HashStorage()
        {
        }

        public void Add(Hash item, int id)
        {
            _rw.AcquireWriterLock(2000);
            _hash2id.Add(item, id);
            _id2hash.Add(id, item);
            _rw.ReleaseWriterLock();
        }

        public void RemoveByHash(Hash item)
        {
            _rw.AcquireWriterLock(2000);
            int id = -1;
            if (_hash2id.TryGetValue(item, out id))
            {
                _hash2id.Remove(item);
                _id2hash.Remove(id);
            }
            _rw.ReleaseWriterLock();
        }

        public void ReplaceById(int id, Hash newHash)
        {
            _rw.AcquireWriterLock(2000);
            Hash hash;
            if (_id2hash.TryGetValue(id, out hash))
            {
                _id2hash[id] = newHash;
                _hash2id.Remove(hash);
                _hash2id.Add(newHash, id);
            }
            _rw.ReleaseWriterLock();
        }

        public void RemoveById(int id)
        {
            _rw.AcquireWriterLock(2000);
            Hash hash;
            if (_id2hash.TryGetValue(id, out hash))
            {
                _id2hash.Remove(id);
                _hash2id.Remove(hash);
            }
            _rw.ReleaseWriterLock();
        }

        public bool ContainsHash(Hash item) => _hash2id.ContainsKey(item);

        public Hash GetById(int id) => _id2hash[id];

        public int GetByHash(Hash hash) => _hash2id[hash];

        public bool TryGetById(int id, out Hash hash) => _id2hash.TryGetValue(id, out hash);

        public bool TryGetByHash(Hash hash, out int id) => _hash2id.TryGetValue(hash, out id);
    }
}
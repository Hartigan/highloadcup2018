using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace AspNetCoreWebApi.Storage.StringPools
{
    public class HashStorage
    {
        private ConcurrentDictionary<int, int> _hash2id = new ConcurrentDictionary<int, int>();
        private ConcurrentDictionary<int, int> _id2hash = new ConcurrentDictionary<int, int>();

        public HashStorage()
        {
        }

        public void Add(int item, int id)
        {
            _hash2id.TryAdd(item, id);
            _id2hash.TryAdd(id, id);
        }

        public void RemoveByHash(int item)
        {
            int id = -1;
            if (_hash2id.TryRemove(item, out id))
            {
                int hash = -1;
                _id2hash.Remove(id, out hash);
            }
        }

        public void RemoveById(int id)
        {
            int hash = -1;
            if (_id2hash.TryRemove(id, out hash))
            {
                _hash2id.Remove(hash, out id);
            }
        }

        public bool ContainsHash(int item) => _hash2id.ContainsKey(item);

        public int GetById(int id) => _id2hash[id];

        public int GetByHash(int hash) => _hash2id[hash];

        public bool TryGetById(int id, out int hash) => _id2hash.TryGetValue(id, out hash);

        public bool TryGetByHash(int hash, out int id) => _hash2id.TryGetValue(hash, out id);
    }
}
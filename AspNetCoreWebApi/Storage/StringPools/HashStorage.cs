using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using AspNetCoreWebApi.Processing;

namespace AspNetCoreWebApi.Storage.StringPools
{
    public struct Hash
    {
        public ushort H41;
        public ushort H43;
        public ushort H47;
        public ushort H53;

        public bool IsNotEmpty()
        {
            return H41 != 0 || H43 != 0 || H47 != 0 || H53 != 0;
        }
    }

    public class HashStorage
    {
        private readonly Dictionary<Hash, int> _hash2id = new Dictionary<Hash, int>(DataConfig.MaxId);
        private readonly Hash[] _id2hash = new Hash[DataConfig.MaxId];

        public HashStorage()
        {
        }

        public void Add(Hash item, int id)
        {
            _hash2id.Add(item, id);
            _id2hash[id] = item;
        }

        public void RemoveByHash(Hash item)
        {
            int id = -1;
            if (_hash2id.TryGetValue(item, out id))
            {
                _hash2id.Remove(item);
                _id2hash[id] = new Hash();
            }
        }

        public void ReplaceById(int id, Hash newHash)
        {
            if (_id2hash[id].IsNotEmpty())
            {
                Hash hash = _id2hash[id];
                _id2hash[id] = newHash;
                _hash2id.Remove(hash);
                _hash2id.Add(newHash, id);
            }
        }

        public void RemoveById(int id)
        {
            if (_id2hash[id].IsNotEmpty())
            {
                Hash hash = _id2hash[id];
                _id2hash[id] = new Hash();
                _hash2id.Remove(hash);
            }
        }

        public bool ContainsHash(Hash item) => _hash2id.ContainsKey(item);

        public Hash GetById(int id) => _id2hash[id];

        public int GetByHash(Hash hash) => _hash2id[hash];

        public bool TryGetByHash(Hash hash, out int id) => _hash2id.TryGetValue(hash, out id);
    }
}
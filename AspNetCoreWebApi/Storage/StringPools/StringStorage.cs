using System;
using System.Collections.Generic;
using AspNetCoreWebApi.Domain;

namespace AspNetCoreWebApi.Storage.StringPools
{
    public class StringStorage
    {
        private IdGenerator _idGenerator = new IdGenerator();
        private Dictionary<short, string> _id2str = new Dictionary<short, string>();
        private Dictionary<string, short> _str2id = new Dictionary<string, short>();

        public StringStorage()
        {
        }

        public short Get(string item)
        {
            if (_str2id.ContainsKey(item))
            {
                return _str2id[item];
            }

            short id = _idGenerator.Get();
            string str = String.Intern(item);
            _id2str.Add(id, str);
            _str2id.Add(str, id);
            
            return id;
        }

        public string GetString(short id) => _id2str[id];
        public short GetId(string str) => _str2id[str];

        public bool TryGet(string str, out short id)
        {
            return _str2id.TryGetValue(str, out id);
        }
    }
}
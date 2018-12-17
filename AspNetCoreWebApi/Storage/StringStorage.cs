using System;
using System.Collections.Generic;
using AspNetCoreWebApi.Domain;

namespace AspNetCoreWebApi.Storage
{
    public class StringStorage
    {
        private IdGenerator _idGenerator = new IdGenerator();
        private SortedDictionary<int, string> _id2str = new SortedDictionary<int, string>();
        private SortedDictionary<string, int> _str2id = new SortedDictionary<string, int>();

        public StringStorage()
        {
        }

        public int Get(string item)
        {
            if (_str2id.ContainsKey(item))
            {
                return _str2id[item];
            }

            int id = _idGenerator.Get();
            string str = String.Intern(item);
            _id2str.Add(id, str);
            _str2id.Add(str, id);
            
            return id;
        }

        public string GetString(int id) => _id2str[id];
        public int GetId(string str) => _str2id[str];
    }
}
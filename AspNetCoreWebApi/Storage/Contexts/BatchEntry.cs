using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public struct BatchEntry<T>
    {
        public BatchEntry(int id, T value)
        {
            Id = id;
            Value = value;
        }

        public int Id;
        public T Value;
    }
}

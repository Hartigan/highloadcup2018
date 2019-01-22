using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public interface IBatchLoader<T>
    {
        void LoadBatch(int id, T item);
    }
}

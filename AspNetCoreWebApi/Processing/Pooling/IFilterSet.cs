using System.Collections;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public interface IFilterSet
    {
        void Add(int id);
        void Remove(int id);
        bool Contains(int id);
        BitArray GetBitArray();
        void Clear();
    }
}
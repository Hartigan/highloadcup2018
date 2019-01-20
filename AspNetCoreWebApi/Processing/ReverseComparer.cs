using System.Collections.Generic;

namespace AspNetCoreWebApi.Processing
{
    public class ReverseComparer<T> : IComparer<T>
    {
        public static ReverseComparer<T> Default { get; } = new ReverseComparer<T>(Comparer<T>.Default);

        private readonly IComparer<T> _source;

        public ReverseComparer(IComparer<T> source)
        {
            _source = source;
        }

        public int Compare(T x, T y)
        {
            return _source.Compare(y, x);
        }
    }
}
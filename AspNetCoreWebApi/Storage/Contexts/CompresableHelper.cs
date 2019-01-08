using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public static class CompresableHelper
    {
        public static void Compress<T>(this List<T> list)
        {
            list.Capacity = list.Count;
        }
    }
}

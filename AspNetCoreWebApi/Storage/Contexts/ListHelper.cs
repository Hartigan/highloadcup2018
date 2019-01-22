using AspNetCoreWebApi.Processing;
using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public static class ListHelper
    {
        public static void SortedInsert(this List<int> list, int id)
        {
            int index = list.BinarySearch(id, ReverseComparer<int>.Default);
            if (index < 0)
            {
                list.Insert(~index, id);
            }
        }

        public static bool SortedRemove(this List<int> list, int id)
        {
            int index = list.BinarySearch(id, ReverseComparer<int>.Default);
            if (index >= 0)
            {
                list.RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

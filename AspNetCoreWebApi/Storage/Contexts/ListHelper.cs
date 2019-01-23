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

        public static void FilterSort(this List<int> list)
        {
            list.Sort(ReverseComparer<int>.Default);
        }

        public static bool FilterSearch(this List<int> list, int id)
        {
            return list.BinarySearch(id, ReverseComparer<int>.Default) >= 0;
        }

        public static IEnumerable<int> MergeSort(List<IEnumerator<int>> enumerators)
        {
            for (int i = 0; i < enumerators.Count;)
            {
                if (!enumerators[i].MoveNext())
                {
                    enumerators.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            while (enumerators.Count > 0)
            {
                int maxIndex = 0;
                for (int i = 1; i < enumerators.Count; i++)
                {
                    if (enumerators[maxIndex].Current < enumerators[i].Current)
                    {
                        maxIndex = i;
                    }
                }

                yield return enumerators[maxIndex].Current;

                if (!enumerators[maxIndex].MoveNext())
                {
                    enumerators.RemoveAt(maxIndex);
                }
            }
        }
    }
}

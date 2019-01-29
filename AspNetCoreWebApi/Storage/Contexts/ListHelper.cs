using AspNetCoreWebApi.Processing;
using System;
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

        public static IEnumerable<T> MergeSort<T>(List<IEnumerator<T>> enumerators, IComparer<T> comparer)
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
                    if (comparer.Compare(enumerators[maxIndex].Current, enumerators[i].Current) > 0)
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

        public static IEnumerable<T> TakeMax<T>(
            this IEnumerable<T> list,
            IComparer<T> comparer,
            int limit)
        {
            SortedSet<T> result = new SortedSet<T>(comparer);

            foreach(var id in list)
            {
                if (result.Count < limit)
                {
                    result.Add(id);
                    continue;
                }

                if (comparer.Compare(result.Max, id) > 0)
                {
                    result.Remove(result.Max);
                    result.Add(id);
                }
            }

            return result;
        }

        public static int Count<T>(this List<T> list, Func<T, bool> predicate)
        {
            int count = 0;
            for(int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    count++;
                }
            }
            return count;
        }
    }
}

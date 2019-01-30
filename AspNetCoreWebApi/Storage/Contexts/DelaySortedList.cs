using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AspNetCoreWebApi.Processing;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class DelaySortedList<T> : IEnumerable<T>
    {

        public static DelaySortedList<int> CreateDefault() => new DelaySortedList<int>(ReverseComparer<int>.Default);

        private List<T> _data = new List<T>(1);
        private HashSet<T> _toRemove;
        private SortedSet<T> _toAdd;
        private readonly IComparer<T> _comparer;

        public DelaySortedList(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public T this[int index] { get => _data[index]; set => _data[index] = value; }

        public int Count => _data.Count;

        public bool IsReadOnly => false;

        public List<T> GetList() => _data;

        public IComparer<T> Comparer => _comparer;

        public void Clear()
        {
            _data.Clear();
            _toRemove = null;
            _toAdd = null;
        }

        public bool FullContains(T item) => Contains(item) || (_toAdd != null && _toAdd.Contains(item));

        public bool Contains(T item) => _data.BinarySearch(item, _comparer) >= 0;

        public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();

        public void DelayAdd(T item)
        {
            if (_toRemove != null && _toRemove.Contains(item))
            {
                _toRemove.Remove(item);
                return;
            }

            if (_toAdd == null)
            {
                _toAdd = new SortedSet<T>(_comparer);
            }

            _toAdd.Add(item);
        }

        public bool DelayRemove(T item)
        {
            if (_toAdd != null && _toAdd.Contains(item))
            {
                _toAdd.Remove(item);
                return true;
            }

            if (_toRemove == null)
            {
                _toRemove = new HashSet<T>(1);
            }
            
            if (!_toRemove.Contains(item) && _data.BinarySearch(item, _comparer) >= 0)
            {
                _toRemove.Add(item);
                return true;
            }

            return false;
        }

        public void Flush()
        {
            if ((_toAdd?.Count ?? 0) == 0 &&
                (_toRemove?.Count ?? 0) == 0)
            {
                if (_data.Capacity > _data.Count)
                {
                    _data.TrimExcess();
                }
                return;
            }

            List<T> newData = new List<T>(_data.Count + (_toAdd?.Count ?? 0) - (_toRemove?.Count ?? 0));
            List<IEnumerator<T>> mergeSort = new List<IEnumerator<T>>(2);

            if (_toRemove != null && _toRemove.Count > 0)
            {
                mergeSort.Add(_data.Where(x => !_toRemove.Contains(x)).GetEnumerator());
                if (_toAdd != null && _toAdd.Count > 0)
                {
                    mergeSort.Add(_toAdd.Where(x => !_toRemove.Contains(x)).GetEnumerator());
                }
            }
            else
            {
                mergeSort.Add(_data.GetEnumerator());
                mergeSort.Add(_toAdd.GetEnumerator());
            }

            IEnumerable<T> merged = ListHelper.MergeSort(mergeSort, _comparer);
            var enumerator = merged.GetEnumerator();

            foreach(var item in merged)
            {
                newData.Add(item);
            }

            _data = newData;
            _toAdd = null;
            _toRemove = null;
        }

        public void Insert(int index, T item)
        {
            if (_data.Count == _data.Capacity)
            {
                if (_data.Count < 10)
                {
                    _data.Capacity += 1;
                }
                else
                {
                    _data.Capacity += 2;
                }
            }

            _data.Insert(index, item);
        }

        public void Load(T item)
        {
            if (_data.Count == _data.Capacity)
            {
                if (_data.Count < 10)
                {
                    _data.Capacity += 1;
                }
                else
                {
                    _data.Capacity = _data.Count * 4 / 3;
                }
            }
            _data.Add(item);
        }

        public void LoadEnded()
        {
            _data.Sort(_comparer);
            _data.TrimExcess();
        }

        public T Find(T eq)
        {
            return _data[_data.BinarySearch(eq, _comparer)];
        }

        public void UpdateOrAdd(T eq, Func<T, T> update)
        {
            if (_toAdd != null)
            {
                T item;
                if (_toAdd.TryGetValue(eq, out item))
                {
                    _toAdd.Remove(item);
                    _toAdd.Add(update(item));
                    return;
                }
            }

            int index = _data.BinarySearch(eq, _comparer);
            if (index >= 0)
            {
                _data[index] = update(_data[index]);
            }
            else
            {
                if (_toAdd == null)
                {
                    _toAdd = new SortedSet<T>(_comparer);
                }
                _toAdd.Add(eq);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
    }
}
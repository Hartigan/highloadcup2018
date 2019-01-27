using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AspNetCoreWebApi.Processing;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class DelaySortedList : IEnumerable<int>
    {
        private List<int> _data = new List<int>();
        private readonly HashSet<int> _toRemove = new HashSet<int>(0);
        private readonly SortedSet<int> _toAdd = new SortedSet<int>(ReverseComparer<int>.Default);

        public DelaySortedList()
        {
        }

        public int this[int index] { get => _data[index]; set => _data[index] = value; }

        public int Count => _data.Count;

        public bool IsReadOnly => false;

        public List<int> GetList() => _data;

        public void Clear()
        {
            _data.Clear();
            _toRemove.Clear();
            _toAdd.Clear();
        }

        public bool Contains(int item) => _data.FilterSearch(item);

        public IEnumerator<int> GetEnumerator() => _data.GetEnumerator();

        public void DelayAdd(int item)
        {
            if (_toRemove.Contains(item))
            {
                _toRemove.Remove(item);
                return;
            }

            _toAdd.Add(item);
        }

        public bool DelayRemove(int item)
        {
            if (_toAdd.Contains(item))
            {
                _toAdd.Remove(item);
                return true;
            }

            if (!_toRemove.Contains(item) && _data.FilterSearch(item))
            {
                _toRemove.Add(item);
                return true;
            }

            return false;
        }

        public void Flush()
        {
            List<int> newData = new List<int>(_data.Count + _toAdd.Count - _toRemove.Count);
            List<IEnumerator<int>> mergeSort = new List<IEnumerator<int>>(2)
            {
                _data.Where(x => !_toRemove.Contains(x)).GetEnumerator(),
                _toAdd.Where(x => !_toRemove.Contains(x)).GetEnumerator()
            };

            IEnumerable<int> merged = ListHelper.MergeSort(mergeSort);
            var enumerator = merged.GetEnumerator();

            foreach(var item in merged)
            {
                newData.Add(item);
            }

            _data = newData;
            _toAdd.Clear();
            _toRemove.Clear();
        }

        public void Load(int item)
        {
            _data.Add(item);
        }

        public void LoadEnded()
        {
            _data.FilterSort();
            _data.TrimExcess();
        }

        IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
    }
}
using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class MergeSortIterator<T> : IIterator<T>
    {
        private readonly IIterator<T> _a;
        private readonly IIterator<T> _b;
        private IIterator<T> _current;
        public T Current => _current.Current;
        public IComparer<T> Comparer => _b.Comparer;
        public bool Completed => _a.Completed && _b.Completed;

        public MergeSortIterator(IIterator<T> a, IIterator<T> b)
        {
            _a = a;
            _b = b;
        }

        public bool MoveNext(T item)
        {
            if (Completed)
            {
                return false;
            }

            if (_current == null)
            {
                if (_a.MoveNext(item))
                {
                    _current = _a;
                }

                if (_b.MoveNext(item))
                {
                    if (_current == null)
                    {
                        _current = _b;
                    }
                    else
                    {
                        if (Comparer.Compare(_current.Current, _b.Current) > 0)
                        {
                            _current = _b;
                        }
                    }
                }

                return _current != null;
            }

            var another = _current == _a ? _b : _a;

            if (_current.MoveNext(item))
            {
                if (another.Completed)
                {
                    return true;
                }

                if (Comparer.Compare(another.Current, item) < 0)
                {
                    if (!another.MoveNext(item))
                    {
                        return true;
                    }
                }

                if (Comparer.Compare(_current.Current, another.Current) > 0)
                {
                    _current = another;
                }

                return true;
            }

            if (another.Completed)
            {
                return false;
            }

            if (Comparer.Compare(another.Current, item) < 0)
            {
                if (another.MoveNext(item))
                {
                    _current = another;
                    return true;
                }
                return false;
            }
            else
            {
                _current = another;
                return true;
            }
        }

        public void Reset()
        {
            _a.Reset();
            _b.Reset();
            _current = null;
        }
    }
}
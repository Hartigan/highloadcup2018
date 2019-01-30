using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class MergeSortIterator : IIterator
    {
        private readonly IIterator _a;
        private readonly IIterator _b;
        private IIterator _current;
        public int Current => _current.Current;
        public bool Completed => _a.Completed && _b.Completed;

        public MergeSortIterator(IIterator a, IIterator b)
        {
            _a = a;
            _b = b;
        }

        public bool MoveNext(int item)
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
                        if (_b.Current - _current.Current > 0)
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

                if (item - another.Current < 0)
                {
                    if (!another.MoveNext(item))
                    {
                        return true;
                    }
                }

                if (another.Current - _current.Current > 0)
                {
                    _current = another;
                }

                return true;
            }

            if (another.Completed)
            {
                return false;
            }

            if (item - another.Current < 0)
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
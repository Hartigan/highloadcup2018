using System.Collections.Generic;

namespace AspNetCoreWebApi.Storage.Contexts
{
    public class EmptyIterator : IIterator
    {
        public int Current => throw new System.NotImplementedException();
        public bool Completed => true;
        public EmptyIterator()
        {
        }

        public bool MoveNext(int item)
        {
            return false;
        }
        public void Reset()
        {
        }
    }
}
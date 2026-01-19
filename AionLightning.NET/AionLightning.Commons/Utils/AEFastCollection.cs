using System;
using System.Collections.Generic;

namespace AionLightning.Commons.Utils
{
    public abstract class AEFastCollection<T> : ICollection<T>
    {
        public abstract void Add(T item);
        public abstract void Clear();
        public abstract bool Contains(T item);
        public abstract void CopyTo(T[] array, int arrayIndex);
        public abstract int Count { get; }
        public abstract bool IsReadOnly { get; }
        public abstract bool Remove(T item);
        public abstract IEnumerator<T> GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

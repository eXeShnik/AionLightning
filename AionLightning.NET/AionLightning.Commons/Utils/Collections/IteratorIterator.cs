using System;
using System.Collections.Generic;

namespace AionLightning.Commons.Utils.Collections
{
    public class IteratorIterator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<IEnumerable<T>> _firstLevelIterator;
        private IEnumerator<T> _secondLevelIterator;

        public IteratorIterator(IEnumerable<IEnumerable<T>> enumerable)
        {
            _firstLevelIterator = enumerable.GetEnumerator();
        }

        public T Current => _secondLevelIterator.Current;

        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_secondLevelIterator != null && _secondLevelIterator.MoveNext())
                return true;

            while (_firstLevelIterator.MoveNext())
            {
                var iterable = _firstLevelIterator.Current;

                if (iterable != null)
                {
                    _secondLevelIterator = iterable.GetEnumerator();

                    if (_secondLevelIterator.MoveNext())
                        return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            _firstLevelIterator.Reset();
            _secondLevelIterator = null;
        }

        public void Dispose()
        {
            _firstLevelIterator.Dispose();
            _secondLevelIterator?.Dispose();
        }
    }
}

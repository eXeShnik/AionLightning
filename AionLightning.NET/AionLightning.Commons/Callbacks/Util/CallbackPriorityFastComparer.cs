using System.Collections.Generic;

namespace AionLightning.Commons.Callbacks.Util
{
    public class CallbackPriorityFastComparer : IEqualityComparer<ICallback<object>>, IComparer<ICallback<object>>
    {
        private readonly CallbackPriorityComparer _comparer = new();

        public bool Equals(ICallback<object> x, ICallback<object> y)
        {
            return _comparer.Compare(x, y) == 0;
        }

        public int GetHashCode(ICallback<object> obj)
        {
            return obj.GetHashCode();
        }

        public int Compare(ICallback<object> x, ICallback<object> y)
        {
            return _comparer.Compare(x, y);
        }
    }
}
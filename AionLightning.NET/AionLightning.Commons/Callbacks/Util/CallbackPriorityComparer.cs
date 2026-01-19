using System.Collections.Generic;

namespace AionLightning.Commons.Callbacks.Util
{
    public class CallbackPriorityComparer : IComparer<ICallback<object>>
    {
        public int Compare(ICallback<object> x, ICallback<object> y)
        {
            var p1 = CallbacksUtil.GetCallbackPriority(x);
            var p2 = CallbacksUtil.GetCallbackPriority(y);

            if (p1 < p2)
            {
                return -1;
            }
            else if (p1 == p2)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
}
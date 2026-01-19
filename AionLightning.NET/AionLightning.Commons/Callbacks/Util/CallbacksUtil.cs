using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AionLightning.Commons.Callbacks.Metadata;

namespace AionLightning.Commons.Callbacks.Util
{
    public static class CallbacksUtil
    {
        public static bool IsAttributePresent(MethodInfo method, Type attribute)
        {
            return method.GetCustomAttributes(attribute, true).Any();
        }

        public static int GetCallbackPriority(ICallback<object> callback)
        {
            if (callback is ICallbackPriority instancePriority)
            {
                return ICallbackPriority.DefaultPriority - instancePriority.Priority;
            }
            else
            {
                return ICallbackPriority.DefaultPriority;
            }
        }

        public static void InsertCallbackToList(ICallback<object> callback, IList<ICallback<object>> list)
        {
            var callbackPriority = GetCallbackPriority(callback);

            if (list.Any())
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var c = list[i];
                    var cPrio = GetCallbackPriority(c);

                    if (callbackPriority < cPrio)
                    {
                        list.Insert(i, callback);
                        return;
                    }
                }
            }

            list.Add(callback);
        }
    }
}
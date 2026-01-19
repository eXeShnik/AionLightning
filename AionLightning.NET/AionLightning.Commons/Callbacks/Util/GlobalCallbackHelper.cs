using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

using AionLightning.Commons.Utils;

namespace AionLightning.Commons.Callbacks.Util
{
    public class GlobalCallbackHelper
    {
        private readonly ILogger<GlobalCallbackHelper> _log;
        private static readonly object _lock = new object();
        private static readonly List<ICallback<object>> _globalCallbacks = new List<ICallback<object>>();

        public GlobalCallbackHelper(ILogger<GlobalCallbackHelper> log)
        {
            _log = log;
        }

        public static void AddCallback(ICallback<object> callback)
        {
            lock (_lock)
            {
                _globalCallbacks.Add(callback);
            }
        }

        public static void RemoveCallback(ICallback<object> callback)
        {
            lock (_lock)
            {
                _globalCallbacks.Remove(callback);
            }
        }

        public static void InvokeCallbacks(object[] args)
        {
            lock (_lock)
            {
                foreach (var cb in _globalCallbacks)
                {
                    cb.Invoke(args);
                }
            }
        }

        public CallbackResult BeforeCall(object obj, Type callbackClass, params object[] args)
        {
            CallbackResult cr = null;
            var callbacks = GetCallbacksSnapshot();

            foreach (var cb in callbacks)
            {
                if (!ClassUtils.IsSubclass(cb.GetBaseClass(), callbackClass))
                {
                    continue;
                }

                try
                {
                    cr = cb.BeforeCall(obj, args);
                    if (cr.IsBlockingCallbacks())
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Exception in global callback");
                }
            }

            return cr ?? new CallbackResult(true);
        }

        public CallbackResult AfterCall(object obj, Type callbackClass, object[] args, object result)
        {
            CallbackResult cr = null;
            var callbacks = GetCallbacksSnapshot();

            foreach (var cb in callbacks)
            {
                if (!ClassUtils.IsSubclass(cb.GetBaseClass(), callbackClass))
                {
                    continue;
                }

                try
                {
                    cr = cb.AfterCall(obj, args, result);
                    if (cr.IsBlockingCallbacks())
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Exception in global callback");
                }
            }

            return cr ?? new CallbackResult(true);
        }

        private static ICallback<object>[] GetCallbacksSnapshot()
        {
            lock (_lock)
            {
                return _globalCallbacks.ToArray();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

using AionLightning.Commons.Utils;

namespace AionLightning.Commons.Callbacks.Util
{
    public class ObjectCallbackHelper
    {
        private readonly ILogger<ObjectCallbackHelper> _log;
        private readonly Dictionary<object, List<ICallback<object>>> _cbMap = new Dictionary<object, List<ICallback<object>>>();
        private readonly object _callbackLock = new object();

        public ObjectCallbackHelper(ILogger<ObjectCallbackHelper> log)
        {
            _log = log;
        }

        public void AddCallback(object instance, ICallback<object> callback)
        {
            lock (_callbackLock)
            {
                if (!_cbMap.ContainsKey(instance))
                    _cbMap[instance] = new List<ICallback<object>>();
                _cbMap[instance].Add(callback);
            }
        }

        public void RemoveCallback(object instance, ICallback<object> callback)
        {
            lock (_callbackLock)
            {
                if (_cbMap.ContainsKey(instance))
                    _cbMap[instance].Remove(callback);
            }
        }

        public void InvokeCallbacks(object instance, object[] args)
        {
            lock (_callbackLock)
            {
                if (_cbMap.ContainsKey(instance))
                {
                    foreach (var cb in _cbMap[instance])
                    {
                        cb.Invoke(args);
                    }
                }
            }
        }

        public CallbackResult BeforeCall(object obj, Type callbackClass, params object[] args)
        {
            CallbackResult cr = null;
            List<ICallback<object>> list = null;

            lock (_callbackLock)
            {
                _cbMap.TryGetValue(callbackClass, out list);
            }

            if (list == null || !list.Any())
            {
                return new CallbackResult(true);
            }

            foreach (var c in list)
            {
                try
                {
                    cr = c.BeforeCall(obj, args);
                    if (cr.IsBlockingCallbacks())
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Exception in object callback");
                }
            }

            return cr ?? new CallbackResult(true);
        }

        public CallbackResult AfterCall(object obj, Type callbackClass, object[] args, object result)
        {
            CallbackResult cr = null;
            List<ICallback<object>> list = null;

            lock (_callbackLock)
            {
                _cbMap.TryGetValue(callbackClass, out list);
            }

            if (list == null || !list.Any())
            {
                return new CallbackResult(true);
            }

            foreach (var c in list)
            {
                try
                {
                    cr = c.AfterCall(obj, args, result);
                    if (cr.IsBlockingCallbacks())
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Exception in object callback");
                }
            }

            return cr ?? new CallbackResult(true);
        }
    }
}
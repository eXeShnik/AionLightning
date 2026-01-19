using System;

namespace AionLightning.Commons.Callbacks.Metadata
{
    [AttributeUsage(AttributeTargets.Method)]
    public class GlobalCallbackAttribute : Attribute
    {
        public Type CallbackType { get; }

        public GlobalCallbackAttribute(Type callbackType)
        {
            CallbackType = callbackType;
        }
    }
}
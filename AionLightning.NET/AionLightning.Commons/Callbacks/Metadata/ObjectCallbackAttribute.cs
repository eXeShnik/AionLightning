using System;

namespace AionLightning.Commons.Callbacks.Metadata
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ObjectCallbackAttribute : Attribute
    {
        public Type CallbackType { get; }

        public ObjectCallbackAttribute(Type callbackType)
        {
            CallbackType = callbackType;
        }
    }
}
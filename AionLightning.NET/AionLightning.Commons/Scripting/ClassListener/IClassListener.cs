using System;

namespace AionLightning.Commons.Scripting.ClassListener
{
    public interface IClassListener
    {
        void PostLoad(Type[] classes);
        void PreUnload(Type[] classes);
    }
}

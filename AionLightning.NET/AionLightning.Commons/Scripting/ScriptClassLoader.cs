using System;
using System.Reflection;
using System.Runtime.Loader;

namespace AionLightning.Commons.Scripting
{
    public class ScriptClassLoader : AssemblyLoadContext
    {
        public ScriptClassLoader() : base(isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}

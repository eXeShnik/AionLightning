using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AionLightning.Commons.Scripting
{
    public class CompilationResult
    {
        private readonly Type[] _compiledClasses;
        private readonly ScriptClassLoader _classLoader;

        public CompilationResult(Type[] compiledClasses, ScriptClassLoader classLoader)
        {
            _compiledClasses = compiledClasses;
            _classLoader = classLoader;
        }

        public ScriptClassLoader GetClassLoader()
        {
            return _classLoader;
        }

        public Type[] GetCompiledClasses()
        {
            return _compiledClasses;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("CompilationResult");
            sb.Append("{classLoader=").Append(_classLoader);
            sb.Append(", compiledClasses=")
                .Append(_compiledClasses == null ? "null" : string.Join(", ", _compiledClasses.Select(c => c.FullName)));
            sb.Append('}');
            return sb.ToString();
        }
    }
}

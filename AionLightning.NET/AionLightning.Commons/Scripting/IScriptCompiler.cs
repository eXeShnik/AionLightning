using System.IO;

namespace AionLightning.Commons.Scripting
{
    public interface IScriptCompiler
    {
        void SetParentClassLoader(ScriptClassLoader classLoader);
        void SetLibraries(System.Collections.Generic.IEnumerable<FileInfo> files);
        CompilationResult Compile(string className, string sourceCode);
        CompilationResult Compile(string[] className, string[] sourceCode);
        CompilationResult Compile(System.Collections.Generic.IEnumerable<FileInfo> compilationUnits);
        string[] GetSupportedFileTypes();
    }
}

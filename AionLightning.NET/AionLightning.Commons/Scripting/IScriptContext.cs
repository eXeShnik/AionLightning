using System.Collections.Generic;
using System.IO;
using AionLightning.Commons.Scripting.ClassListener;

namespace AionLightning.Commons.Scripting
{
    public interface IScriptContext
    {
        void Init();
        void Shutdown();
        void Reload();
        DirectoryInfo GetRoot();
        CompilationResult GetCompilationResult();
        bool IsInitialized();
        void SetLibraries(IEnumerable<FileInfo> files);
        IEnumerable<FileInfo> GetLibraries();
        IScriptContext GetParentScriptContext();
        ICollection<IScriptContext> GetChildScriptContexts();
        void AddChildScriptContext(IScriptContext context);
        void SetClassListener(IClassListener cl);
        IClassListener GetClassListener();
        void SetCompilerClassName(string className);
        string GetCompilerClassName();
    }
}

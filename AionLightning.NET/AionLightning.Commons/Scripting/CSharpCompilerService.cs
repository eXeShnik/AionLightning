using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;

namespace AionLightning.Commons.Scripting;

public sealed class CSharpCompilerService(ILogger<CSharpCompilerService> log)
{
    public (AssemblyLoadContext Alc, Assembly Assembly)? Compile(string sourceCode, string name)
        => CompileTrees([CSharpSyntaxTree.ParseText(sourceCode)], name);

    /// <summary>
    /// Batch-compile mode: parses every *.cs file under <paramref name="folder"/> (recursively)
    /// into a single <see cref="CSharpCompilation"/> — one assembly, one collectible ALC — instead
    /// of the per-file isolation <see cref="Compile"/> uses. Lets a folder of hand-written scripts
    /// (e.g. quest handlers) reference each other and be discovered together via reflection.
    /// Returns null if the folder has no *.cs files or the batch fails to compile.
    /// </summary>
    public (AssemblyLoadContext Alc, Assembly Assembly)? CompileFolder(string folder, string name)
    {
        var files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
        if (files.Length == 0) return null;

        var trees = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f));
        return CompileTrees(trees, name);
    }

    private (AssemblyLoadContext Alc, Assembly Assembly)? CompileTrees(IEnumerable<SyntaxTree> trees, string name)
    {
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: name,
            syntaxTrees: trees,
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                log.LogError("Compile error in {Name}: {Message}", name, d.ToString());
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        var alc = new AssemblyLoadContext(name, isCollectible: true);
        var asm = alc.LoadFromStream(ms);
        return (alc, asm);
    }
}

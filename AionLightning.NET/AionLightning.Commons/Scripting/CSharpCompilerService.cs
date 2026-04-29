using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;

namespace AionLightning.Commons.Scripting;

public sealed class CSharpCompilerService(ILogger<CSharpCompilerService> log)
{
    public (AssemblyLoadContext Alc, Assembly Assembly)? Compile(string sourceCode, string name)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);

        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: name,
            syntaxTrees: [tree],
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

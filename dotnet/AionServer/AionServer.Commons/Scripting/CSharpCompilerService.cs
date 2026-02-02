using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;

namespace AionServer.Commons.Scripting;

public class CSharpCompilerService(ILogger<CSharpCompilerService> logger)
{
    public Assembly? CompileFromFile(string filePath, IEnumerable<string>? additionalReferences = null)
    {
        var code = File.ReadAllText(filePath);
        return CompileFromSource(code, additionalReferences);
    }

    public Assembly? CompileFromSource(string sourceCode, IEnumerable<string>? additionalReferences = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = GetDefaultReferences();

        if (additionalReferences != null)
            references = references.Concat(additionalReferences.Select(r => MetadataReference.CreateFromFile(r)));

        var compilation = CSharpCompilation.Create(
            assemblyName: $"DynamicAssembly_{Guid.NewGuid()}",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                logger.LogError("Compilation error: {Message}", diagnostic.ToString());

            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        return AssemblyLoadContext.Default.LoadFromStream(ms);
    }

    private IEnumerable<MetadataReference> GetDefaultReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(Enumerable).Assembly,
            Assembly.Load("System.Runtime"),
            Assembly.Load("System.Collections"),
            Assembly.Load("netstandard")
        };

        return assemblies.Select(a => MetadataReference.CreateFromFile(a.Location));
    }
}
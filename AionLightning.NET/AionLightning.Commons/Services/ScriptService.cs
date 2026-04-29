using AionLightning.Commons.Scripting;
using AionLightning.Commons.Scripting.Contracts;
using Microsoft.Extensions.Logging;
using System.Runtime.Loader;

namespace AionLightning.Commons.Services;

public sealed class ScriptService(CSharpCompilerService compiler, ILogger<ScriptService> log) : IAsyncDisposable
{
    private sealed record LoadedScript(WeakReference<AssemblyLoadContext> AlcRef, IScript Instance);

    private readonly Dictionary<string, LoadedScript> _loaded = new(StringComparer.OrdinalIgnoreCase);

    public async Task LoadAllAsync(string folder, IScriptHost host, CancellationToken ct)
    {
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
            await LoadFileAsync(file, host, ct);
    }

    public async Task ReloadAsync(string path, IScriptHost host, CancellationToken ct)
    {
        await UnloadAsync(path);
        await LoadFileAsync(path, host, ct);
    }

    private async Task LoadFileAsync(string path, IScriptHost host, CancellationToken ct)
    {
        var source = await File.ReadAllTextAsync(path, ct);
        var name = $"Script_{Path.GetFileNameWithoutExtension(path)}_{Guid.NewGuid():N}";

        var compiled = compiler.Compile(source, name);
        if (compiled is null) return;

        var (alc, asm) = compiled.Value;
        var scriptType = asm.GetExportedTypes().FirstOrDefault(t => t.IsAssignableTo(typeof(IScript)) && !t.IsAbstract);
        if (scriptType is null)
        {
            log.LogWarning("No IScript implementation found in {Path}", path);
            alc.Unload();
            return;
        }

        var instance = (IScript)Activator.CreateInstance(scriptType)!;
        await instance.InitializeAsync(host, ct);

        _loaded[path] = new LoadedScript(new WeakReference<AssemblyLoadContext>(alc), instance);
        log.LogInformation("Loaded script: {Path}", path);
    }

    private Task UnloadAsync(string path)
    {
        if (!_loaded.Remove(path, out var loaded)) return Task.CompletedTask;

        if (loaded.AlcRef.TryGetTarget(out var alc))
        {
            alc.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var path in _loaded.Keys.ToList())
            await UnloadAsync(path);
    }
}

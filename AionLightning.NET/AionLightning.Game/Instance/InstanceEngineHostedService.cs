using AionLightning.Commons.Scripting;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Instance;

/// <summary>
/// Boots the instance-handler engine at server start by batch-compiling every <c>*.cs</c> under
/// <c>Scripts/instance/**</c> (Roslyn) and registering each <see cref="GeneralInstanceHandler"/>
/// subclass carrying an <see cref="InstanceIdAttribute"/> as the handler factory for its map — the
/// exact mirror of <c>QuestEngineHostedService.LoadHandWrittenScripts</c>. Handler scripts use a
/// parameterless constructor (Java <c>newInstance()</c>); they reach game services through the
/// static injection wired here via <see cref="GeneralInstanceHandler.InitServices"/>.
/// </summary>
public sealed class InstanceEngineHostedService(
    InstanceEngine engine,
    SpawnService spawnService,
    GameWorld world,
    IDataManager dataManager,
    DoorService doorService,
    CSharpCompilerService compiler,
    ILogger<InstanceEngineHostedService> log) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        GeneralInstanceHandler.InitServices(spawnService, world, dataManager, doorService);

        string scriptsFolder = Path.Combine(AppContext.BaseDirectory, "Scripts", "instance");
        if (!Directory.Exists(scriptsFolder))
        {
            log.LogInformation("InstanceEngine: no Scripts/instance folder found, running with no-op handlers only.");
            return Task.CompletedTask;
        }

        var compiled = compiler.CompileFolder(scriptsFolder, "InstanceScripts");
        if (compiled is null)
        {
            log.LogWarning("InstanceEngine: Scripts/instance batch-compile produced no assembly (empty folder or compile errors — see above).");
            return Task.CompletedTask;
        }

        var (_, asm) = compiled.Value;
        int discovered = 0;

        foreach (var type in asm.GetExportedTypes())
        {
            if (type.IsAbstract || !type.IsAssignableTo(typeof(GeneralInstanceHandler))) continue;

            var attr = (InstanceIdAttribute?)Attribute.GetCustomAttribute(type, typeof(InstanceIdAttribute));
            if (attr is null)
            {
                log.LogWarning("InstanceEngine: script type {Type} has no [InstanceId] attribute, skipping.", type.FullName);
                continue;
            }

            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor is null)
            {
                log.LogWarning("InstanceEngine: script type {Type} has no parameterless constructor, skipping.", type.FullName);
                continue;
            }

            engine.Register(attr.WorldId, () => (IInstanceHandler)ctor.Invoke(null));
            discovered++;
        }

        log.LogInformation("InstanceEngine: registered {Discovered} instance handler(s) from {Folder}", discovered, scriptsFolder);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

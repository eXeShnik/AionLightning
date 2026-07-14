using AionLightning.Commons.Scripting;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Ai;

/// <summary>
/// Boots the NPC AI engine at server start by batch-compiling every <c>*.cs</c> under
/// <c>Scripts/ai/**</c> (Roslyn) and registering each <see cref="NpcAi2"/> subclass carrying an
/// <see cref="AiNameAttribute"/> as the AI factory for its name — the exact mirror of
/// <see cref="Instance.InstanceEngineHostedService"/> for instance handlers. AI scripts use a
/// parameterless constructor; they reach game services through the static injection wired here via
/// <see cref="NpcAi2.InitServices"/>.
/// </summary>
public sealed class AiEngineHostedService(
    AiEngine engine,
    SpawnService spawnService,
    GameWorld world,
    IDataManager dataManager,
    DoorService doorService,
    NpcShoutsService shoutsService,
    CSharpCompilerService compiler,
    ILogger<AiEngineHostedService> log) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        NpcAi2.InitServices(spawnService, world, dataManager, doorService, shoutsService);

        string scriptsFolder = Path.Combine(AppContext.BaseDirectory, "Scripts", "ai");
        if (!Directory.Exists(scriptsFolder))
        {
            log.LogInformation("AiEngine: no Scripts/ai folder found, running with archetype-driven AI only.");
            return Task.CompletedTask;
        }

        var compiled = compiler.CompileFolder(scriptsFolder, "AiScripts");
        if (compiled is null)
        {
            log.LogWarning("AiEngine: Scripts/ai batch-compile produced no assembly (empty folder or compile errors — see above).");
            return Task.CompletedTask;
        }

        var (_, asm) = compiled.Value;
        int discovered = 0;

        foreach (var type in asm.GetExportedTypes())
        {
            if (type.IsAbstract || !type.IsAssignableTo(typeof(NpcAi2))) continue;

            var attr = (AiNameAttribute?)Attribute.GetCustomAttribute(type, typeof(AiNameAttribute));
            if (attr is null)
            {
                log.LogWarning("AiEngine: script type {Type} has no [AiName] attribute, skipping.", type.FullName);
                continue;
            }

            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor is null)
            {
                log.LogWarning("AiEngine: script type {Type} has no parameterless constructor, skipping.", type.FullName);
                continue;
            }

            engine.Register(attr.Name, () => (NpcAi2)ctor.Invoke(null));
            discovered++;
        }

        log.LogInformation("AiEngine: registered {Discovered} AI script(s) from {Folder}", discovered, scriptsFolder);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

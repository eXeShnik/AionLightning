# ADR-006: Scripting engine

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

Java Aion ships runtime-loadable scripts under `AL-Game/data/scripts/` — `.java` files compiled on server start (and optionally hot-reloaded) to define quest logic, NPC behaviour, spawn templates, and one-off handlers. The classloader isolation lets scripts be unloaded and recompiled without restarting the server.

The .NET port has:

- `AionLightning.Commons/Scripting/CSharpCompilerService.cs` — uses `Microsoft.CodeAnalysis.CSharp` correctly: builds a `CSharpCompilation`, emits to a `MemoryStream`, loads through `AssemblyLoadContext.Default`.
- `AionLightning.Commons/Services/ScriptService.cs` — `ScriptManager.Load(FileInfo)` and `Shutdown()` throw `NotImplementedException` (lines 12, 17); `LoadDir` scans `*.xml` (Java `ScriptContext` descriptor carry-over, wrong for `.cs`); no integration with `FolderListenerService`.
- `AionLightning.Commons/Services/FolderListenerService.cs` — working `FileSystemWatcher` wrapper, not consumed by `ScriptService`.

Two bugs are latent:

1. `AssemblyLoadContext.Default` is not collectible — repeated reloads leak assembly memory.
2. Scripts read as `*.xml` means `.cs` files are silently ignored.

## Decision

Keep `Microsoft.CodeAnalysis.CSharp` (the full compiler API). Rewrite `ScriptService` to:

- Scan `.cs` files in a configurable folder.
- Compile them via `CSharpCompilerService` with host assemblies as references.
- Load each compiled assembly into its own **collectible** `AssemblyLoadContext(isCollectible: true)`.
- Expose loaded script types via **host-side interfaces only** (see R-005 in [`../risks.md`](../risks.md)).
- Wire `FolderListenerService` events to trigger incremental recompile + reload.
- Track ALCs by `WeakReference` so unload + GC reliably reclaim memory.

## Alternatives considered

### Alt-1: `Microsoft.CodeAnalysis.CSharp.Scripting`

- Pros: REPL-style `CSharpScript.RunAsync`, automatic reference resolution, `Globals` object for host binding.
- Cons: scripting assemblies are non-collectible and interact poorly with collectible ALCs — exactly the hot-reload case we need (dotnet/roslyn #72366).
- Why rejected: hot reload leaks.

### Alt-2: Precompiled plugin DLLs

- Pros: simplest runtime story, no compiler at runtime.
- Cons: every content change needs a build + deploy; loses the Java experience of editing a `.java` and restarting.
- Why rejected: we want the hot-edit workflow of the Java original.

### Alt-3: Non-collectible `AssemblyLoadContext.Default`

- Pros: what the current port does — simplest.
- Cons: memory grows unbounded with reloads.
- Why rejected: unacceptable for a long-running server.

### Alt-4: Embed a dynamic language (Lua, Python via IronPython)

- Pros: smaller surface than full C#; industry-standard scripting for games.
- Cons: a new language for every script author; we'd need to re-author (not just port) all Aion scripts.
- Why rejected: port cost is too high.

## Consequences

### Positive

- Hot reload works without leak.
- Scripts are real C# — same language, IDE, debugger as the host.
- Host interfaces in Commons give a stable contract that does not cross ALC boundaries.

### Negative / trade-offs

- Scripts must only reference **interface types** exposed by the host (via `AionLightning.Commons.Scripting.Contracts` or similar). Trying to use `Player` directly crosses ALC identity — see [R-005](../risks.md).
- The `data/scripts/**/*.java` catalogue must be ported or regenerated as `.cs`. Volume unknown — see [R-006](../risks.md).
- Collectible ALCs have restrictions: some reflection patterns, some P/Invoke scenarios, and certain COM interop do not work. Not expected to be a problem for Aion scripts but captured here.

## Implementation notes

### Contracts

Host exposes interfaces in Commons:

```csharp
// AionLightning.Commons.Scripting.Contracts
public interface IScript
{
    ValueTask InitializeAsync(IScriptHost host, CancellationToken ct);
}

public interface IScriptHost
{
    IEventBus Events { get; }
    IWorld World { get; }
    ILogger<IScript> Logger { get; }
    // ... minimal surface scripts are allowed to touch
}
```

Scripts implement `IScript`. At load time, the service scans the compiled assembly for `IScript` implementations and instantiates them.

### Compile + load pipeline

```csharp
public sealed class ScriptService(
    CSharpCompilerService compiler,
    FolderListenerService watcher,
    ILogger<ScriptService> log) : IAsyncDisposable
{
    private readonly Dictionary<string, LoadedScript> _loaded = new();

    public async Task LoadAllAsync(string folder, CancellationToken ct) { /* ... */ }
    public async Task ReloadAsync(string path, CancellationToken ct) { /* ... */ }

    private sealed record LoadedScript(WeakReference<AssemblyLoadContext> Alc, IScript Instance);
}
```

### `CSharpCompilerService` changes

- Emit to `MemoryStream` (already done).
- Load into a new `AssemblyLoadContext(name, isCollectible: true)` — **not** `.Default`.
- Add metadata references for all host assemblies loaded into `AppDomain.CurrentDomain.GetAssemblies()` (this is how scripts get `IScriptHost`, `IEventBus`, etc.).

### Unload

- Drop the instance, call `alc.Unload()`, force `GC.Collect()` twice.
- Assert the `WeakReference` target is null in tests (M7).
- Document that event subscriptions must be disposed before unload, or they keep the ALC alive.

### `FolderListenerService` removal

- The current `Services/ScriptService.cs:70-76` `LoadDir` scans `*.xml`. Replace with `.cs` scan.
- Subscribe `watcher.Changed` / `Created` / `Deleted` to call `ReloadAsync(path)`.

## Affected milestones

- M1: `ScriptService` stays untouched (still broken); we only rely on its compiler piece for reference, not load.
- M5: `ScriptService` rewrite lands here — first real consumer is the world-entry flow.
- M6.4 (quests): heaviest script workload.

## Related

- ADR-005 (Callbacks): scripts subscribe to the event bus through host interfaces.
- Risks: [R-005](../risks.md), [R-006](../risks.md).

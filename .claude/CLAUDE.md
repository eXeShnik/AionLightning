# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This repository contains **two parallel codebases**:

1. **Legacy Java (Java 1.7, Ant)** — the original Aion Lightning 4.6.2 server: `AL-Commons/`, `AL-Login/`, `AL-Chat/`, `AL-Game/`. Read-only reference — the migration source of truth.
2. **Target .NET 10 solution** — `AionLightning.NET/` with four projects that mirror the Java modules. All new work goes here.

The current branch `dot_net_10_migration` is an in-progress port from Java to .NET 10 console apps. Status is tracked in `migration_plan.md` (authoritative — read before starting module-level work).

## .NET Solution Layout

```
AionLightning.NET/
├── Directory.Build.props           # sets DotNetVersion=net10.0, ExtensionsVersion=10.0.2
├── AionLightning.NET.sln
├── AionLightning.Commons/          # shared lib (Callbacks, Configuration, Database, Network, Scripting, Services, Utils, TaskManager, Versioning)
├── AionLightning.Login/            # console app — client auth + gameserver registry
├── AionLightning.Chat/             # console app — chat packets (skeleton only)
└── AionLightning.Game/             # console app — main gameplay packets (skeleton only)
```

Only `AionLightning.Login` currently has migrated logic beyond a `Program.cs` stub. `Chat` and `Game` are empty `Host.CreateDefaultBuilder` scaffolds.

## Commands

All .NET commands run from `AionLightning.NET/`:

```bash
dotnet build AionLightning.NET.sln                        # build whole solution
dotnet build AionLightning.Login/AionLightning.Login.csproj
dotnet run --project AionLightning.Login
dotnet run --project AionLightning.Chat
dotnet run --project AionLightning.Game
```

No test project exists yet in the .NET solution. The legacy Java module has JUnit tests under `AL-Commons/test/` — these are not ported and are reference-only.

The root-level `.bat` / `unix_build_*.sh` scripts build the **Java** codebase via Ant — they are not used for .NET work.

## Architecture — .NET Side

### Hosting & DI
Every server project uses `Host.CreateDefaultBuilder(args)` with:
- An `IHostedService` implementation as the server entrypoint (e.g. `LoginServer : IHostedService` — `AionLightning.Login/LoginServer.cs`).
- Services registered via `services.AddSingleton<T>()` / `AddHostedService<T>()` in `Program.cs`.
- Serilog via `.UseSerilog(...)` reading from `appsettings.json` `Serilog` section, writing to console + rolling file in `log/`.
- `ILogger<T>` injected via constructor — never use a static logger.

### Configuration
- `appsettings.json` per-project (see `AionLightning.Login/appsettings.json`). Keys preserve the legacy Java dotted names (`loginserver.network.client.port`, etc.).
- Legacy-style static config loaders exist (`AionLightning.Login/Configs/Config.cs`) but the migration direction is toward `IConfiguration` + DI.
- The `Database` section in `appsettings.json` still carries a JDBC-format URL from the Java original — rewrite to a real MySQL connection string when wiring `DatabaseFactory`.

### Networking
The Java NIO layer is ported structurally to `AionLightning.Commons/Network/`:
- `NioServer` — accepts multiple `ServerCfg`s (one per listener, e.g. Aion client + GameServer).
- `AcceptDispatcherImpl` / `AcceptReadWriteDispatcherImpl` / `Dispatcher` — per-thread accept + r/w loops.
- `AConnection` / `AionConnection` — connection base classes; `IConnectionFactory` produces them per accepted socket.
- `AionPacket` / `AionClientPacket` / `AionServerPacket` — packet abstractions; per-server `ClientPackets`/`ServerPackets` folders hold protocol messages.

Per `migration_plan.md`, this layer still uses raw `Socket` and must be replaced with `TcpListener`/`TcpClient` before moving forward on Chat/Game.

### Scripting
`AionLightning.Commons/Scripting/CSharpCompilerService.cs` compiles `.cs` sources at runtime via Roslyn (`Microsoft.CodeAnalysis.CSharp`). `FolderListenerService` (`AionLightning.Commons/Services/`) watches a directory with `FileSystemWatcher` for live reload. The `AionLightning.Login.csproj` is configured to **copy** `Scripts/**/*.cs` to output but **remove them from compilation** (`RemoveScriptsFromBuild` target) so scripts remain source files executed at runtime. Replicate that csproj pattern when adding scripting to Chat/Game.

### Callbacks / AOP
`AionLightning.Commons/Callbacks/` holds placeholder ports of the Java agent-based AOP (`ICallback`, `IEnhancedObject`, `DotNetAgentEnhancer`). Per `migration_plan.md`, this subsystem is explicitly **under review** — the Java `AgentEnhancer` approach likely won't translate cleanly and may be dropped or replaced with source generators / Castle.Core. Do not build on top of these placeholders without clarifying direction first.

### Database
`AionLightning.Commons/Database/` contains `DatabaseFactory`, `DB`, `Transaction`, and handler interfaces (`IReadStH`, `IIUStH`, `IParamReadStH`, `ICallReadStH`). Backed by `MySql.Data` (v8.2.0). Per-server DAO classes live under `AionLightning.Login/Dao/`. Services is still TODO per the migration plan.

## Key Dependencies

Set centrally in `Directory.Build.props`:
- `net10.0` target, `Microsoft.Extensions.*` pinned to `10.0.2`.

Per-project packages of note:
- Commons: `Microsoft.CodeAnalysis.CSharp` 5.0.0, `BouncyCastle.Cryptography` 2.6.2, `MySql.Data` 8.2.0, `Quartz` 3.15.1.
- Login: `Serilog.Extensions.Hosting`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `Microsoft.Extensions.Hosting.WindowsServices`.

Use `BouncyCastle.Cryptography` for all crypto (session keys, blowfish, RSA — see `AionLightning.Login/Network/Ncrypt/`). Do not reach for `System.Security.Cryptography` equivalents when Bouncy already handles the Java original's primitive.

## Migration Workflow Conventions

- The Java package `com.aionemu.commons.*` → .NET namespace `AionLightning.Commons.*`; `com.aionemu.loginserver.*` → `AionLightning.Login.*`; and similarly for `chatserver`/`gameserver`.
- Java `synchronized` / `Runnable` / `ExecutorService` → review against `Microsoft.Extensions.*` primitives first; don't port Java concurrency verbatim.
- When a Java class pulls in Java-1.7-era utility libs (e.g. `slf4j`, custom reflection helpers, Java concurrency collections), evaluate whether the .NET BCL already supplies the behavior before porting. Record discardable Java-only pieces in `migration_plan.md` so they aren't re-introduced.
- `migration_plan.md` is the single source of truth for module progress — update the checkbox list when you finish a subtree (e.g. Commons `services`, `taskmanager`, `versionning`).

## Important Notes From `migration_plan.md`

- Migration is based on the **4.6.2** variant of the Java source, not 7.8.
- The Java `Tools/` directory (bundled Ant etc.) is intentionally out of scope.
- Preserve migration history: summarize non-obvious migration decisions in the plan or an adjacent note rather than discarding them.

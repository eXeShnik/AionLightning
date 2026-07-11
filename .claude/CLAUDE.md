# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This repository contains **two parallel codebases**:

1. **Legacy Java (Java 1.7, Ant)** — the original Aion Lightning 4.6.0 server: `AL-Commons/`, `AL-Login/`, `AL-Chat/`, `AL-Game/`. Read-only reference — the migration source of truth.
2. **Target .NET 10 solution** — `AionLightning.NET/` with four projects that mirror the Java modules. All new work goes here.

The current branch `dot_net_10_migration` is an in-progress port from Java to .NET 10 console apps. Status is tracked in `migration_plan.md` (authoritative — read before starting module-level work).

## .NET Solution Layout

```
AionLightning.NET/
├── Directory.Build.props           # sets DotNetVersion=net10.0, ExtensionsVersion=10.0.2
├── AionLightning.NET.sln
├── AionLightning.Commons/          # shared lib (Configuration, Database, Events, Hosting, Network, Objects, Scripting, Services, TaskManager, Utils, Versioning)
├── AionLightning.Login/            # console app — client auth + gameserver registry (~90% of Java parity; verified end-to-end with real client)
├── AionLightning.Chat/             # console app — chat server (~80%; full packet parity, not yet validated against a real client)
└── AionLightning.Game/             # console app — main gameplay server (~35%; core loop playable, see gaps below)
```

All four projects carry substantial migrated logic. Game status (as of 2026-07-11): 181 client packet handlers (Java parity), 137/231 server packets, extensive skill-effect coverage (milestones M1–M380+ in `migration_plan.md`), working core loop (login → char CRUD → world → movement → combat → XP → items → groups → legion → loot → craft → broker → quests-basic). Major gaps: geodata engine (absent), scriptable quest engine (basic XML-driven only), instances/sieges/housing (absent/skeletal), NPC AI (single service vs Java `ai2` framework), flight system (partial).

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
- An `IHostedService` implementation as the server entrypoint (e.g. `LoginServerHost` in `AionLightning.Login`).
- Services registered via `services.AddSingleton<T>()` / `AddHostedService<T>()` in `Program.cs`.
- Serilog via `.UseSerilog(...)` reading from `appsettings.json` `Serilog` section, writing to console + rolling file in `log/`.
- `ILogger<T>` injected via constructor — never use a static logger.

### Configuration
- `appsettings.json` per-project (see `AionLightning.Login/appsettings.json`), bound to `*Options` records via `AddAionOptions<T>("Section:Key")` and consumed through `IOptions<T>`.

### Networking
`AionLightning.Commons/Network/` holds the shared layer (the Java NIO dispatcher design was **not** ported verbatim):
- `AConnection` — connection base class (async read loop over a `Socket` accepted by `TcpListener`); `IConnectionFactory<T>` produces connections per accepted socket.
- `AionPacket` / `AionClientPacket` / `AionServerPacket`, `PacketReader` / `PacketWriter` — packet abstractions; per-server `ClientPackets`/`ServerPackets` folders hold protocol messages.
- Each server has its own `IHostedService` listener host (`LoginServerHost`, `ChatServerHost`, `GameServerHost`) built on `TcpListener`.

Wire-format notes (hard-won, don't rediscover): length prefix is a little-endian int16 **including** the 2 prefix bytes; server→client packets end with a checksum dword; client→server packets carry their checksum at `length-8` with 4 trailing padding bytes; the first server packet (SM_INIT) is EncXorPass'd + static-Blowfish instead of checksummed. The C# login server's output was byte-diff-verified identical to Java's.

### Scripting
`AionLightning.Commons/Scripting/CSharpCompilerService.cs` compiles `.cs` sources at runtime via Roslyn (`Microsoft.CodeAnalysis.CSharp`). `FolderListenerService` (`AionLightning.Commons/Services/`) watches a directory with `FileSystemWatcher` for live reload. The `AionLightning.Login.csproj` is configured to **copy** `Scripts/**/*.cs` to output but **remove them from compilation** (`RemoveScriptsFromBuild` target) so scripts remain source files executed at runtime. Replicate that csproj pattern when adding scripting to Chat/Game.

### Callbacks / AOP
The Java agent-based AOP (`AgentEnhancer`, `ICallback`) was **dropped** — there is no `Callbacks/` folder in Commons. Events flow through `AionLightning.Commons/Events/` (`IEventBus`) instead. Do not re-port the Java callback system.

### Database
Data access uses **`MySqlConnector` + `Dapper`** (not `MySql.Data`). Schema migrations run at startup via **Evolve** (`SchemaMigrationHost`; SQL files like `V2__items.sql` per project). Per-server DAO interfaces + `*DaoImpl` classes live under each project's `Dao/`; register one singleton per DAO in `Program.cs`. Connection strings live in `appsettings.json` `ConnectionStrings` (real MySQL format, not JDBC).

## Key Dependencies

Set centrally in `Directory.Build.props`:
- `net10.0` target, `Microsoft.Extensions.*` pinned to `10.0.2`.

Per-project packages of note:
- Commons: `Microsoft.CodeAnalysis.CSharp` 5.0.0, `BouncyCastle.Cryptography` 2.6.2, `MySqlConnector` 2.3.7, `Dapper` 2.1.35, `Evolve` 3.1.0, `Quartz` 3.15.1, `Serilog.*`.
- Server projects: `Microsoft.Extensions.Hosting(.WindowsServices)`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`.

Use `BouncyCastle.Cryptography` for all crypto (session keys, blowfish, RSA — see `AionLightning.Login/Network/Ncrypt/`). Do not reach for `System.Security.Cryptography` equivalents when Bouncy already handles the Java original's primitive.

## Migration Workflow Conventions

- The Java package `com.aionemu.commons.*` → .NET namespace `AionLightning.Commons.*`; `com.aionemu.loginserver.*` → `AionLightning.Login.*`; and similarly for `chatserver`/`gameserver`.
- Java `synchronized` / `Runnable` / `ExecutorService` → review against `Microsoft.Extensions.*` primitives first; don't port Java concurrency verbatim.
- When a Java class pulls in Java-1.7-era utility libs (e.g. `slf4j`, custom reflection helpers, Java concurrency collections), evaluate whether the .NET BCL already supplies the behavior before porting. Record discardable Java-only pieces in `migration_plan.md` so they aren't re-introduced.
- `migration_plan.md` is the single source of truth for module progress — update the checkbox list when you finish a subtree (e.g. Commons `services`, `taskmanager`, `versionning`).

## Important Notes From `migration_plan.md`

- Migration is based on the **4.6.0** variant of the Java source, not 7.8.
- The Java `Tools/` directory (bundled Ant etc.) is intentionally out of scope.
- Preserve migration history: summarize non-obvious migration decisions in the plan or an adjacent note rather than discarding them.

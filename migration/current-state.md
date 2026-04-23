# Current state of the .NET port

Honest reassessment of what is actually built in `AionLightning.NET/` at the start of the v2 plan. This file anchors the drift of `migration_plan.md` vs. reality.

## Commons summary

| Subsystem | v1 claim | Reality | v2 verdict |
|---|---|---|---|
| `Utils/` | `[✓]` | Rnd, MTRandom, Base64, NetworkUtils, ClassUtils — working. A few `Collections/`, `Concurrent/` — partial. | keep |
| `Objects/` | `[✓]` | Filter — a minimal skeleton. | keep |
| `Options/` | `[✓]` | Assertion — single file, OK. | keep |
| `Configuration/` | `[✓]` | PropertyTransformers + factory — working. | keep |
| `Callbacks/` | `[✓]` | Stub. `DotNetAgentEnhancer.Initialize()` = `Console.WriteLine("... needs to be implemented ...")`. | drop — [ADR-005](adr/005-callbacks.md) |
| `Database/` | `[✓]` | `DB.cs` — synchronous ADO.NET, no `using`, no async, `Close(DbConnection)` has no null check (potential NRE). Handler interfaces instead of Dapper. | rewrite — [ADR-004](adr/004-database.md) |
| `Network/` | `[✓]` | `Dispatcher.Run`: `while(true){ Dispatch(); Thread.Sleep(1); } catch(Exception e){ log; }` — spin loop with swallow. `Shutdown()` = `// TODO: implement`. `Executor` — custom implementation on `Monitor.Wait/Pulse`. No async / `CancellationToken` anywhere. | rewrite — [ADR-001](adr/001-networking.md) |
| `Scripting/` | `[✓]` | `CSharpCompilerService` uses Roslyn correctly and calls `LoadFromStream`. **But:** the ALC is non-collectible (memory leak on reload); `ScriptService.Load(FileInfo)` throws `NotImplementedException`; `LoadDir` scans `*.xml` (a Java copy-paste). | rewrite — [ADR-006](adr/006-scripting.md) |
| `Services/` | in-progress | `FolderListenerService` — working and idiomatic. `ScriptService` — stub. `CronService`, `Threading/*` — partial. | partial |
| `TaskManager/` | todo | Single file `AbstractLockManager.cs`. Almost certainly unnecessary in .NET (DI + `SemaphoreSlim` usually covers the same ground). | evaluate and drop |
| `Versioning/` | todo | Single stub. | evaluate and drop |

## Login

| Layer | Reality |
|---|---|
| `Configs/Config.cs` | Static singleton: `public static string LOGIN_BIND_ADDRESS { get; private set; }`, etc. Breaks DI. → rewrite ([ADR-002](adr/002-configuration.md)) |
| `LoginServer.cs` (`IHostedService.StartAsync` → `OnStarted`) | Half of the method is commented out (`// Config.Load();`, `// DatabaseFactory.Init(...)`, `// DAOManager.Init();`, `// CronService.Init(...)`). Active calls: `ThreadPoolManager.GetInstance()`, `KeyGen.Init()`, `BannedIpController.Load()`, `GameServerTable.Load()`, `_netConnector.Connect()`. |
| `Program.cs` | `Host.CreateDefaultBuilder` + Serilog + DI for `LoginServer`, `NetConnector`, `AionPacketHandlerFactory`, `GsPacketHandlerFactory`. Switched to `HostApplicationBuilder` ([ADR-003](adr/003-hosting.md)). |
| `Network/Aion/*`, `Network/GameServer/*` | Structural skeletons. The packet class list is incomplete (see `AL-Login/src/com/aionemu/loginserver/network/aion/clientpackets` for the full inventory). |
| `Network/Ncrypt/*` | `BlowfishCipher`, `CryptEngine`, `EncryptedRSAKeyPair`, `KeyGen` — present. Moved to `Commons/Network/Ncrypt/` in M1 (needed by both Chat and Game as well). |
| `Dao/*` | `AccountDAO`, `AccountTimeDAO`, `BannedIpDAO`, `BannedMacDAO`, `PremiumDAO` — files exist but sit on top of the raw `DB.cs`. Rewritten on Dapper in M2. |
| `Controller/*` | `AccountController`, `BannedIpController`, `BannedMacManager` — skeletal. |
| `Model/*` | `Account`, `AccountTime`, `BannedIP`, `ReconnectingAccount` — present. |
| `GameServerTable.cs`, `GameServerInfo.cs`, `PingPongThread.cs` | Present. `PingPongThread` is Java-style; replaced by `PeriodicTimer` in M3. |
| `appsettings.json` | Contains all Java config keys + Serilog section + Database. `Database.Url` is in JDBC format (`jdbc:mysql://...`); reformatted to a standard MySQL connection string in M1. |

## Chat

- `Program.cs` — bare `Host.CreateDefaultBuilder().Build().RunAsync()` with `AddConsole()`.
- No other files.
- csproj references Commons + Hosting only.
- Scope — M4.

## Game

- `Program.cs` — same shape as Chat.
- No other files.
- csproj — analogous.
- Scope — M5 and M6.

## Quantitative estimate

- Commons: ~80% of structural files present, functionally ~50% due to stubs/rewrite items.
- Login: ~50% skeleton, ~25–30% functional.
- Chat: 0%.
- Game: 0%.
- Overall vs. Aion 4.6.2 functionality: **~30–35%**.

## Artefacts kept as-is

- `AionLightning.NET/AionLightning.NET.sln`.
- `AionLightning.NET/Directory.Build.props` (only `ExtensionsVersion` may need bumping).
- `appsettings.json` — keys kept, only the reading mechanism changes.
- `AionLightning.Commons/Utils/{Rnd,MTRandom,Base64,NetworkUtils,ClassUtils,AEInfos,ExitCode,SystemPropertyUtil}.cs`.
- `AionLightning.Commons/Configuration/*` (transformers + attribute + processor).
- `AionLightning.Login/Model/*`, `AionLightning.Login/Network/Ncrypt/*`.
- `AionLightning.Commons/Scripting/CSharpCompilerService.cs` — only the wrapper in `ScriptService` is rewritten.
- `AionLightning.Commons/Services/FolderListenerService.cs` — wired up into `ScriptService`.

## Known defects captured explicitly

- `AionLightning.Commons/Callbacks/DotNetAgentEnhancer.cs:16-25` — stub.
- `AionLightning.Commons/Services/ScriptService.cs:12,17` — `NotImplementedException` in `ScriptManager.Load/Shutdown`; line 72 — `LoadDir` scans `*.xml` (Java copy-paste).
- `AionLightning.Commons/Network/Dispatcher.cs:30-48` — `Shutdown = TODO`, spin loop with swallowed exception, blocking `Monitor.Wait/Pulse`.
- `AionLightning.Commons/Database/DB.cs:18-173` — synchronous, no async, no `using`, no cancellation, `Close(DbConnection)` without null check.
- `AionLightning.Login/Configs/Config.cs` — static singleton.
- `AionLightning.Login/LoginServer.cs:48-95` — half of `OnStarted` is commented out.

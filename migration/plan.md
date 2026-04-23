# Overall migration plan

## 1. Goal and strategy

Rewrite four Java servers (Commons, Login, Chat, Game) on top of .NET 10. The Java code is a read-only reference; once the migration is finished, Java is not used at runtime.

**Strategy: code-driven rewrite.** Read a Java class → write the idiomatic .NET class (async, DI, Pipelines, record-based config). Packet layouts, SQL and game constants are ported 1:1 from Java 4.6.0. Runtime coexistence of Java and .NET is not used, even during development.

**Java source of truth.** `/tmp/aion-refs/4.6.0/` — dedicated git worktree from `origin/4.6.0`. The current working tree is branch `7.8.0` and must not be read for port references. See [`conventions.md`](conventions.md) and [`agent-rules.md`](agent-rules.md) Rule 1.

**Verification.** During a milestone — manual smoke testing against the Aion 4.6.0 client. Automated tests and packet golden traces are a separate milestone (M7), not a gate for earlier work.

## 2. Principles

- **DoD for a milestone is verified by a real run.** `NotImplementedException` in the public API of a closed-milestone subsystem is not acceptable.
- **Idiomatic .NET by default.** A 1:1 Java pattern is only acceptable with a justification captured in the milestone file or an ADR.
- **Async / CancellationToken everywhere.** No blocking I/O, no `new Thread().Start()` in new subsystems.
- **DI instead of static.** Static classes with public setters are forbidden; config via `IOptions<T>`, services via constructor injection.
- **One decision per subsystem.** If `MySqlConnector` is chosen, `MySql.Data` is not kept in parallel.
- **Vertical-slice milestones.** Every milestone ends with a working end-to-end scenario, not a "set of classes".

## 3. Current-state reassessment

Detailed in [`current-state.md`](current-state.md). Summary:

| .NET port area | v1 status | Reality | v2 verdict |
|---|---|---|---|
| Commons/utils | [✓] | working | `keep` |
| Commons/objects, options | [✓] | minimal, working | `keep` |
| Commons/configuration | [✓] | transformers OK, but `Login/Configs/Config.cs` is a static anti-pattern | `partial → rewrite` ([ADR-002](adr/002-configuration.md)) |
| Commons/database | [✓] | synchronous ADO.NET, no `using`, no pool | `rewrite` ([ADR-004](adr/004-database.md)) |
| Commons/network | [✓] | `while(true)+Thread.Sleep(1)+swallow Exception`, `Shutdown = TODO` | `rewrite` ([ADR-001](adr/001-networking.md)) |
| Commons/callbacks | [✓] | `Console.WriteLine("needs to be implemented")` | `drop` ([ADR-005](adr/005-callbacks.md)) |
| Commons/scripting | [✓] | compiler OK, `ScriptService.Load` = `NotImplementedException`, scans `*.xml` | `partial → rewrite` ([ADR-006](adr/006-scripting.md)) |
| Commons/services, taskmanager, versioning | in-progress / TODO | partial / stub | `partial` / `todo` |
| Login | TODO | ~50% skeleton, `OnStarted` commented out | `in-progress` (M2) |
| Chat | TODO | `Program.cs` skeleton | `todo` (M4) |
| Game | TODO | `Program.cs` skeleton | `todo` (M5–M6) |

## 4. Milestones

The order is a sequence of vertical slices. Each milestone ends with a scenario that can be demonstrated to the client.

| # | Name | Demo scenario | Scope file | Execution brief |
|---|---|---|---|---|
| M1 | Commons Core MVP | Host boots, logs, DB answers `SELECT 1`, graceful shutdown | [scope](milestones/m1-commons-core.md) | [brief](milestones/m1-execution-brief.md) |
| M2 | Login server: client auth | 4.6.0 client logs in, sees a stubbed serverlist | [scope](milestones/m2-login-auth.md) | [brief](milestones/m2-execution-brief.md) |
| M3 | Login ↔ GameServer handshake | Client selects server, switches to GS socket, GS accepts handoff | [scope](milestones/m3-login-gs-handshake.md) | [brief](milestones/m3-execution-brief.md) |
| M4 | Chat server | `/shout` flows between two clients via Chat | [scope](milestones/m4-chat-server.md) | [brief](milestones/m4-execution-brief.md) |
| M5 | Game: world entry | Client enters the world, sees its character, walks, logs out | [scope](milestones/m5-game-world-entry.md) | [brief](milestones/m5-execution-brief.md) |
| M6 | Game content | AI / skills / items / quests / instances / PvP — sub-milestones | [scope](milestones/m6-game-content.md) | [brief](milestones/m6-execution-brief.md) |
| M7 | Verification harness | Unit + integration tests + packet golden traces + CI | [scope](milestones/m7-verification-harness.md) | [brief](milestones/m7-execution-brief.md) |

Each milestone has **two files**:

- `mN-*.md` — scope, DoD, out-of-scope, smoke scenario.
- `mN-execution-brief.md` — step-by-step work plan: file order, concrete code patterns, Java source references with line numbers, verification steps. **Agents read the brief first.**

## 5. Architectural decisions

| ADR | Topic | Decision | File |
|---|---|---|---|
| 001 | Networking | `System.IO.Pipelines` + async `Socket`, little-endian framing | [adr/001](adr/001-networking.md) |
| 002 | Configuration | `IOptions<T>` with `record` DTOs, `ValidateOnStart` | [adr/002](adr/002-configuration.md) |
| 003 | Hosting | `Host.CreateApplicationBuilder` + `BackgroundService` | [adr/003](adr/003-hosting.md) |
| 004 | Database | `MySqlConnector` + Dapper + `MySqlDataSource` | [adr/004](adr/004-database.md) |
| 005 | Callbacks | Bespoke `Channel<T>`-based event bus | [adr/005](adr/005-callbacks.md) |
| 006 | Scripting | `Microsoft.CodeAnalysis.CSharp` + collectible `AssemblyLoadContext` | [adr/006](adr/006-scripting.md) |
| 007 | Schema migration | Evolve (SQL-file versioning) | [adr/007](adr/007-schema-migration.md) |

Cross-cutting specs that agents must read:

- [`agent-rules.md`](agent-rules.md) — execution rules for automated agents.
- [`conventions.md`](conventions.md) — project conventions (naming, `.editorconfig`, references).
- [`packet-buffer-adapter.md`](packet-buffer-adapter.md) — full `PacketReader` / `PacketWriter` API.
- [`data-schema.md`](data-schema.md) — DB schema, connection strings, password hash.
- [`glossary.md`](glossary.md) — Java → .NET idiom mappings.

## 6. Dependencies between milestones

```
M1 ──┬──> M2 ──> M3 ──> M4 ──> M5 ──> M6 ──> M7
     │          (LS/GS handshake)
     └── ADR-001..004, 007 locked down here
         ADR-005, 006 activate in M5
```

- M1 locks in the architectural choices (networking, config, hosting, DB, schema migration). Changes after M1 are expensive.
- M2 uses all five foundational ADRs from M1; ADR-005 / 006 are not needed yet.
- M3 reuses the networking layer from M2 and adds a second listener on the same code.
- M4 is a separate process but shares all M1 ADRs.
- M5 is the first real use of callbacks (ADR-005) and scripting (ADR-006).
- M6 extends the architecture set up in M5.
- M7 is a test harness built on top of everything already in place.

## 7. Kept from the current port

- `AionLightning.NET.sln` and `Directory.Build.props` (`net10.0`, `ExtensionsVersion=10.0.2`).
- `appsettings.json` key values — preserved. Structure reorganised per [ADR-002](adr/002-configuration.md).
- Serilog integration in `AionLightning.Login/Program.cs`.
- `Commons/Utils/` (Rnd, MTRandom, Base64, NetworkUtils, ClassUtils) — math, OK.
- `Commons/Objects/`, `Commons/Options/` — minimal, OK.
- `CSharpCompilerService` — the compiler itself stays; the wrapper is rewritten.
- `BouncyCastle.Cryptography` as the single crypto library. `Login/Network/Ncrypt/` moves into Commons without logic changes.
- DAO / Controller / Model skeletons in Login — naming templates, functional code rewritten.
- Folder layout `Login/Network/{Aion,GameServer,Factories,Ncrypt}` — kept.

## 8. Dropped

- `Commons/Network/NioServer.cs`, `Dispatcher.cs`, `AcceptDispatcherImpl.cs`, `AcceptReadWriteDispatcherImpl.cs`, `Executor.cs`, `Acceptor.cs`, `DisconnectionTask.cs`, `DisconnectionThreadPool.cs`, `PacketProcessor.cs` — the full Java NIO port.
- `Commons/Callbacks/*` — no AOP reproduced ([ADR-005](adr/005-callbacks.md)).
- `Commons/Database/DB.cs` in its current shape (sync, no `using`).
- `Login/Configs/Config.cs` (static singleton).
- `Login/PingPongThread.cs` (replaced by `PeriodicTimer`-based service in M3).
- Handler interfaces `IReadStH`, `IIUStH`, `IParamReadStH`, `ICallReadStH` — replaced by Dapper.
- `AionLightning.Commons/TaskManager/AbstractLockManager.cs` — evaluated and dropped; DI + `SemaphoreSlim` replaces it.

## 9. Deliberately not done

- No Java-agent AOP is reproduced in any form.
- No parallel runtime of a Java server and a .NET server against the same world.
- No attempt to close Commons to 100% before Login starts. The Commons MVP in M1 is only what Login needs; the rest is added as later milestones demand.
- No premature optimisation. The first goal is functional parity. Performance profiling happens after M6.
- No test suite in M1–M6 as a gate. The test harness is M7, a dedicated milestone.

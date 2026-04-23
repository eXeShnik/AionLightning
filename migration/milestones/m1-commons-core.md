# M1 — Commons Core MVP

Status: `[ ]` not started · Requires: — · ADRs: 001, 002, 003, 004

## Goal

Turn `AionLightning.Commons` into a usable foundation for Login: hosting, config, logging, DB, networking base, crypto. No functional server yet — the milestone ends with a smoke program that proves every foundation piece works.

## Scope

### In

- Hosting helper (`Host.CreateApplicationBuilder` wrapper) — [ADR-003](../adr/003-hosting.md).
- Configuration: record DTOs + `BindConfiguration` + `ValidateOnStart` — [ADR-002](../adr/002-configuration.md).
- Logging: Serilog reading from `appsettings.json:Serilog` — keep existing behaviour.
- Database: `MySqlDataSource` singleton + Dapper — [ADR-004](../adr/004-database.md).
- Networking base: async `AConnection`, `AionPacket`, `AionClientPacket`, `AionServerPacket`, framing via `SequenceReader<byte>`, packet buffer adapter (`ReadC/ReadH/ReadD/ReadQ/ReadS`) — [ADR-001](../adr/001-networking.md).
- Crypto: move `Login/Network/Ncrypt/*` into `Commons/Network/Ncrypt/` (shared by Login, GS, Chat).
- Utilities kept: `Rnd`, `MTRandom`, `Base64`, `NetworkUtils`, `ClassUtils`, `AEInfos`, `ExitCode`, `SystemPropertyUtil`.

### Out of scope

- Any listener / accept loop (moves to M2).
- Callbacks (ADR-005 dropped in Commons).
- Scripting (ADR-006 rewritten in M5).
- TaskManager, Versioning — evaluated, most likely dropped.
- DAOs for business data — only a smoke DAO in M1.

## Dependencies

- Previous milestones: none.
- ADRs activated: 001, 002, 003, 004.
- Open questions that must be closed: Q-1 (Java 4.6.0 reference access), Q-4 (schema migration tool).

## Java source inventory

Taken at milestone start:

```bash
find AL-Commons/src/com/aionemu/commons -name "*.java" | wc -l
find AL-Commons/src/com/aionemu/commons/network -name "*.java"
find AL-Commons/src/com/aionemu/commons/database -name "*.java"
find AL-Commons/src/com/aionemu/commons/utils -name "*.java"
```

Reference-only; we do not port the NIO/Dispatcher layer (see [ADR-001](../adr/001-networking.md)).

## .NET target layout

### New / rewritten

- `AionLightning.Commons/Hosting/AionHostBuilder.cs` — shared builder helper.
- `AionLightning.Commons/Configuration/AionOptionsExtensions.cs` — `AddAionOptions<T>(section)` helper.
- `AionLightning.Commons/Database/AionDataSourceExtensions.cs` — `AddAionDataSource(connStringKey)` helper.
- `AionLightning.Commons/Network/AConnection.cs` — rewritten, async, `PipeReader`/`PipeWriter`, `CancellationToken`.
- `AionLightning.Commons/Network/AionPacket.cs`, `AionClientPacket.cs`, `AionServerPacket.cs` — rewritten.
- `AionLightning.Commons/Network/PacketBuffer.cs` — `SequenceReader`/`IBufferWriter` adapter with `ReadC/ReadH/ReadD/ReadQ/ReadS` shortcuts.
- `AionLightning.Commons/Network/Ncrypt/*` — moved from Login.
- `AionLightning.Commons/Network/IConnectionFactory.cs` — reshaped to async.
- `AionLightning.Commons/Network/ServerCfg.cs` — keep with minor shape update.
- `AionLightning.Commons/Network/IPRange.cs` — keep.

### Removed

- `AionLightning.Commons/Network/NioServer.cs`
- `AionLightning.Commons/Network/Dispatcher.cs` (incl. `Executor` class inside)
- `AionLightning.Commons/Network/AcceptDispatcherImpl.cs`
- `AionLightning.Commons/Network/AcceptReadWriteDispatcherImpl.cs`
- `AionLightning.Commons/Network/Acceptor.cs` — if the new design replaces it
- `AionLightning.Commons/Network/DisconnectionTask.cs`, `DisconnectionThreadPool.cs`
- `AionLightning.Commons/Network/PacketProcessor.cs` — replaced by the pipelines-based dispatcher
- `AionLightning.Commons/Database/DB.cs`
- `AionLightning.Commons/Database/IReadStH.cs`, `IIUStH.cs`, `IParamReadStH.cs`, `ICallReadStH.cs`
- `AionLightning.Commons/Callbacks/*` — entire folder
- `AionLightning.Login/Configs/Config.cs` — static singleton

### csproj changes

`AionLightning.Commons.csproj`:

- Add `MySqlConnector` (latest stable).
- Add `Dapper` (latest stable).
- Remove `MySql.Data`.
- Remove `System.Data.SqlClient`.
- Keep `BouncyCastle.Cryptography`, `Microsoft.Extensions.*`, `Microsoft.CodeAnalysis.CSharp`, `Quartz`.

`AionLightning.Login.csproj`:

- Keep Serilog references, remove commented-out scripts block if unused by M1.

## Definition of Done

- [ ] `dotnet build AionLightning.NET.sln` — zero warnings.
- [ ] No `NotImplementedException` in any M1-scope public API.
- [ ] `appsettings.json` restructured per [ADR-002](../adr/002-configuration.md); the existing flat keys are converted to sections.
- [ ] `AionLightning.Login` starts as a smoke host: reads config (validated), opens DB connection, runs `SELECT 1` successfully, logs a structured line, stops cleanly on Ctrl-C within < 2 seconds.
- [ ] Ncrypt classes live under `Commons/Network/Ncrypt/` and compile without reference errors.
- [ ] Removed files list (above) is actually removed — verified by `git status`.
- [ ] Schema-migration tool decision is recorded in [`data-schema.md`](../data-schema.md) and the tool is integrated.

## Smoke scenario

1. Ensure a local MySQL has the `al_server_ls` database with the 4.6.2 schema applied (see [`data-schema.md`](../data-schema.md)).
2. `cd AionLightning.NET/AionLightning.Login && dotnet run`.
3. Expected console output:
   - "Loaded NetworkOptions"
   - "Database connection OK (SELECT 1 -> 1)"
   - "Ready."
4. Press `Ctrl-C`. Expected: "Shutting down…", process exits with code 0 within 2s.
5. Introduce a deliberate config error (wrong port type) → startup fails with a clear validation message, process exits non-zero.

## Scoped risks

- **Packet buffer adapter** — see [R-001](../risks.md). If the adapter layer is not a clean enough abstraction, packet ports in M2 will leak ByteBuffer idioms everywhere. Mitigation: write the adapter and port 3 trivial Java packet classes to verify the shape before M2 starts.
- **Ncrypt move breaks Login** — verify references after move.

## Notes

- The Login smoke-host is not the final Login; it is a throwaway `Program.cs` that proves the Commons plumbing. It will be replaced in M2 by the real accept loop.
- Keep the `appsettings.json` key names documented in a comment block at the top of the file so Java-operators can translate `loginserver.network.client.port` → `LoginServer:Network:ClientPort` quickly.

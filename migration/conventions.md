# Project conventions

Short set of rules specific to this project. Global rules (`~/.claude/rules/dotnet.md`, `architecture.md`, `code-quality.md`) are not duplicated. Agents must also read [`agent-rules.md`](agent-rules.md).

## Java reference location

Do **not** read Java sources from the current working tree — that tree is branch `7.8.0`, not the 4.6.0 baseline.

All Java references live in a dedicated git worktree. **Every path in this repo that begins with `/tmp/aion-refs/4.6.0/` refers to that worktree and is a default-location placeholder** — substitute your own path if you set the worktree up elsewhere (e.g. on Windows, or in a durable home-directory folder).

Default layout:

```
/tmp/aion-refs/4.6.0/
├── AL-Commons/
├── AL-Login/
├── AL-Chat/
├── AL-Game/
└── Tools/
```

Setup (once per machine):

```bash
# Linux / macOS (default)
git worktree add /tmp/aion-refs/4.6.0 origin/4.6.0

# Durable alternative (survives reboot)
git worktree add ~/work/aion-refs/4.6.0 origin/4.6.0

# Windows (use forward slashes)
git worktree add C:/temp/aion-refs/4.6.0 origin/4.6.0
```

When running an agent via [`agent-prompt.md`](agent-prompt.md), pass your actual path as the `{JAVA_REF}` placeholder. When reading migration docs by hand, mentally substitute the default `/tmp/aion-refs/4.6.0/` for your chosen location.

## Namespaces

- `AionLightning.Commons.*` — shared library (was Java `com.aionemu.commons.*` + `com.aionl.*`).
- `AionLightning.Login.*` — login server.
- `AionLightning.Chat.*` — chat server.
- `AionLightning.Game.*` — game server.

Sub-namespaces mirror Java sub-packages:

| Java | .NET |
|---|---|
| `com.aionemu.commons.utils` | `AionLightning.Commons.Utils` |
| `com.aionemu.commons.network` | `AionLightning.Commons.Network` |
| `com.aionemu.commons.database` | `AionLightning.Commons.Database` |
| `com.aionemu.loginserver.network.aion.clientpackets` | `AionLightning.Login.Network.Aion.ClientPackets` |
| `com.aionemu.gameserver.world` | `AionLightning.Game.World` |

Package → namespace: PascalCase conversion, hierarchy preserved.

## Class naming

- DAO classes: suffix `Dao` (`AccountDao`), **not** `DAO` (Java caps).
- Controller classes: suffix `Controller`.
- Service classes: suffix `Service`.
- Packet classes: **keep Java names verbatim** (`CM_LOGIN`, `SM_INIT`, `CM_ACCOUNT_AUTH`) so grep across the 4.6.0 source works both ways. Analyzer warnings suppressed via `.editorconfig` (below).
- Event records (ADR-005): suffix `Event` (`PlayerLoggedInEvent`, `HpChangedEvent`).
- Config record DTOs: suffix `Options` (`NetworkOptions`, `DatabaseOptions`).

## `.editorconfig` for packet naming

Drop this block into `AionLightning.NET/.editorconfig` (created in M1):

```ini
root = true

[*.cs]
# general .NET conventions live in sdk defaults

# packet classes keep Java UPPER_SNAKE naming
[**/Network/**/{ClientPackets,ServerPackets}/*.cs]
dotnet_diagnostic.IDE1006.severity = none   # Naming Styles
dotnet_diagnostic.CA1707.severity = none    # Identifiers should not contain underscores
dotnet_diagnostic.SA1300.severity = none    # StyleCop: element must begin with upper-case letter
dotnet_style_naming_rule.types_and_namespaces_should_be_pascal_case.severity = none
```

No other file may suppress these warnings.

## File layout

- One public type per file. File name = type name. For packets, file name matches Java: `CM_LOGIN.cs` contains `public sealed class CM_LOGIN`.
- Only static utility methods may be grouped (`Rnd`, `NetworkUtils`).

## Mandatory `using` patterns

- `using var conn = await dataSource.OpenConnectionAsync(ct);` — every DB call.
- `await using` for `IAsyncDisposable`.
- Connection, command and reader — always wrapped in `using` / `using var`.

## Async rules

- I/O methods are `async Task` / `async ValueTask`. Name suffix `Async` (except for packet handler overrides where the base method name is fixed).
- `CancellationToken` is the last parameter (or first after `this` in extension methods). Never defaulted in new code.
- Never `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `.Task.Wait()`.
- No `async void` except framework event handlers.
- `ConfigureAwait(false)` — **not applied globally**; reviewed before release if needed.

## Logging

- `ILogger<T>` via constructor injection. `T` = containing class.
- Named placeholders: `"{UserId}"`, not `"{0}"`.
- Levels:
  - `Trace` — detailed flow (packet hex dump).
  - `Debug` — expected events (connection opened, packet handled).
  - `Information` — lifecycle (server started/stopping, config loaded).
  - `Warning` — degradation without crash (slow query > 100 ms, flood detected).
  - `Error` — caught and recovered from.
  - `Critical` — error that terminates the process.

## Packet classes

See [`packet-buffer-adapter.md`](packet-buffer-adapter.md) for the full `PacketReader` / `PacketWriter` API. Summary:

- Aion protocol is **little-endian**, strings are **UTF-16LE null-terminated**.
- Java read helpers `readC/H/D/Q/S/B` map to `PacketReader.ReadC/H/D/Q/S/B`.
- Java write helpers `writeC/H/D/Q/S/B` map to `PacketWriter.WriteC/H/D/Q/S/B`.
- Framing: 2-byte little-endian length prefix; opcode is the first byte of the decrypted body.
- Base classes `AionClientPacket` / `AionServerPacket` live in Commons.

## Tests (M7)

- xUnit for unit tests.
- Testcontainers for DB integration with MySQL.
- Golden traces — raw `byte[]` under `Tests/GoldenTraces/<packet-name>.bin`.

## SQL

- DDL scripts from `/tmp/aion-refs/4.6.0/AL-Login/sql/` and `/tmp/aion-refs/4.6.0/AL-Game/sql/` copy into `AionLightning.NET/Sql/{login,game}/` renamed to Evolve's `V{n}__{description}.sql`.
- Schema-migration tool — **Evolve** ([ADR-007](adr/007-schema-migration.md)).

## Comments

- XML doc only on Commons public APIs where the contract is non-obvious.
- `// TODO` — requires a risk or task id: `// TODO (R-007): confirm this packet is from the 4.6.0 branch`.
- `// FIXME` — only when it blocks a merge.

## Git

- Commit messages: imperative mood, short. Milestone prefix is welcome: `M1: add DbDataSource singleton`.
- PRs never mention AI tools; no change stats (rule from `~/.claude/rules/git-operations.md`).

## Secrets

- DB passwords, shared secrets, Blowfish static keys — never in tracked files.
- Local overrides go to `appsettings.Development.json` (gitignored).
- CI / container deployment uses environment variables (`ConnectionStrings__LoginDb=...`).

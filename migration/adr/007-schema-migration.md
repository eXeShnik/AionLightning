# ADR-007: Schema migration tool

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

The Aion Lightning 4.6.2 database schema ships as plain SQL files in `AL-Login/sql/al_server_ls.sql` (169 lines, one file) and `AL-Game/sql/` (multiple files — inventoried at the start of M5).

On startup we need to:

- Apply missing DDL if the database is empty.
- Ensure the schema version is consistent with the running server build.
- Support idempotent re-runs (migrations already applied are skipped).

The Java original manually tells operators to run SQL files by hand — no automation. We do not want to carry that model.

## Decision

Use **Evolve** (<https://evolve-db.netlify.app/>) as the schema migration tool.

- SQL files from `AL-Login/sql/` and `AL-Game/sql/` are copied verbatim into `AionLightning.NET/Sql/{login,game}/` and renamed to Evolve's `V{version}__{description}.sql` convention.
- Evolve runs at server startup (Login and Game each manage their own database) and applies any new migrations.
- The Evolve metadata table tracks applied versions per database.

Startup registration in `LoginServerHost.StartAsync`:

```csharp
var evolve = new Evolve.Evolve(dbConnection, msg => _log.LogInformation("Evolve: {Message}", msg))
{
    Locations = new[] { "Sql/login" },
    IsEraseDisabled = true,
};
evolve.Migrate();
```

## Alternatives considered

### Alt-1: FluentMigrator

- Pros: migrations as C# code → refactor-friendly, compile-time checks, strong up/down story.
- Cons: we would be rewriting every SQL file as C# `Execute.Sql(...)` or building columns / tables fluently. For a ported schema (169 lines and already working), the rewrite is wasted effort and creates drift risk vs. the Java baseline.
- Why rejected: value does not justify the rewrite.

### Alt-2: Raw SQL files run manually

- Pros: zero tooling.
- Cons: no versioning, no record of applied migrations, easy to double-apply, operator burden.
- Why rejected: we are automating a 4.6.2 project, not porting its ops burden.

### Alt-3: DbUp

- Pros: similar SQL-file model, popular in ASP.NET Core shops.
- Cons: a touch more ceremonial than Evolve; no out-of-the-box embedded-resource + file-system-discovery hybrid. Smaller community relative to Evolve for MySQL specifically.
- Why rejected: marginally; Evolve won on fit.

### Alt-4: EF Core Migrations

- Pros: integrates with EF Core model.
- Cons: EF Core is not our data-access layer ([ADR-004](004-database.md) — Dapper). EF Migrations would exist purely for DDL with no model to back them.
- Why rejected: wrong tool — we are not using EF.

## Consequences

### Positive

- Schema changes land through code review; never run by hand.
- Each server owns its database bootstrap; no shared "which DB admin ran what" tribal knowledge.
- Migrations are embedded resources or file-system locations — the existing SQL files are copy-pasted, then renamed, then frozen.

### Negative / trade-offs

- Another dependency (`Evolve.Core` NuGet).
- SQL must be versioned with the `V{n}__` naming. The initial import becomes `V1__initial_4_6_2.sql`.
- No down-migrations (Evolve does not support rollback). Acceptable: we rarely need to reverse a DDL in a fresh project; if we ever do, it is a new `V{n+1}__revert_x.sql`.

## Implementation notes

### File layout

```
AionLightning.NET/
├── Sql/
│   ├── login/
│   │   └── V1__initial_4_6_2.sql   ← verbatim copy of AL-Login/sql/al_server_ls.sql
│   └── game/
│       ├── V1__initial_players.sql
│       ├── V2__initial_inventory.sql
│       └── ...
```

One-time import command at M1 start:

```bash
cp /tmp/aion-refs/4.6.2/AL-Login/sql/al_server_ls.sql \
   AionLightning.NET/Sql/login/V1__initial_4_6_2.sql
# Game: copy each source file; assign increasing V{n} in a stable order.
```

### Packages

- `Evolve.Core` (MIT license).
- Connection-string source: `ConnectionStrings:LoginDb` / `ConnectionStrings:GameDb` (ADR-004).

### Connection string format

The current `appsettings.json` has `Database.Url = jdbc:mysql://localhost:3306/al_server_ls`. Convert to MySqlConnector format:

```
Server=localhost;Port=3306;Database=al_server_ls;Uid=root;Pwd=;AllowPublicKeyRetrieval=true;SslMode=None;
```

### Metadata table

Evolve creates `changelog` (configurable name) in the target database. Do not remove manually.

## Affected milestones

- M1: install Evolve, create `Sql/login/V1__initial_4_6_2.sql`, wire `evolve.Migrate()` into Login startup, verify against an empty DB.
- M3: mirror for Game server once `Sql/game/V1__*.sql` is assembled.
- M4: Chat does not own its own schema — reuses game DB.

## Related

- ADR-004 (Database): provides the connection source used by Evolve.
- [`data-schema.md`](../data-schema.md): lists table groups and sources.

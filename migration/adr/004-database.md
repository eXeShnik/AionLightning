# ADR-004: Database access

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

Java servers talk to MySQL via hand-written JDBC code (`PreparedStatement`, `ResultSet`) wrapped in a `DB` / `DatabaseFactory` façade. DAOs (`AccountDAO`, `PlayerDAO`, ...) extend a base and use handler interfaces (`IReadStH`, `IIUStH`) to process results.

Current .NET port in `AionLightning.Commons/Database/DB.cs`:

- Synchronous (`ExecuteReader`, `ExecuteNonQuery`).
- No `using` — connections are closed in `finally` manually.
- `Close(DbConnection con)` has no null check (potential NRE).
- No connection pooling configuration.
- `MySql.Data` 8.2.0 (Oracle) — async methods exist but are notoriously implemented as sync-over-async internally.

This is a production hazard on a packet-heavy server.

## Decision

**Connector:** `MySqlConnector` (MIT, genuinely async).
**DAO pattern:** Dapper for query execution, `record` DTOs as row shapes.
**Connection factory:** `MySqlDataSource` registered as a DI singleton (built-in pooling).
**Cancellation:** every data method takes a `CancellationToken` and flows it through.

```csharp
public sealed class AccountDao(MySqlDataSource ds)
{
    public async Task<Account?> FindByLoginAsync(string login, CancellationToken ct)
    {
        await using var conn = await ds.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Account>(
            "SELECT id, login, password_hash FROM account_data WHERE login = @login",
            new { login });
    }
}
```

## Alternatives considered

### Alt-1: Keep `MySql.Data` (Oracle)

- Pros: already in `csproj` (8.2.0). No migration cost.
- Cons: async methods are sync-over-async; license GPL-with-FOSS-exception (uncomfortable for proprietary work); slower to track new .NET APIs.
- Why rejected: we block a packet-heavy server on a sync-backed async façade — exactly the scenario the scalability story warns about.

### Alt-2: EF Core + Pomelo

- Pros: full ORM, change tracking, migrations.
- Cons: porting 100+ hand-written Java DAOs into EF entities is a large rewrite with risk of query-shape drift; EF's generated SQL is not always what hand-tuned Aion queries assume.
- Why rejected: the wrong tool for the bulk of this workload. **Optional later** for admin utilities or new aggregates (M6+).

### Alt-3: Raw ADO.NET without Dapper

- Pros: zero dependencies beyond the connector.
- Cons: boilerplate (`DbCommand`, parameter add, reader loop) clones the Java `DB.cs` shape — exactly what we are moving away from.
- Why rejected: Dapper is a ~30 KB library that replaces all that boilerplate with one line.

### Alt-4: Keep the existing handler interfaces

- Pros: preserves Java DAO shape.
- Cons: they are the main reason `DB.cs` is hard to read; Dapper does the same job better.
- Why rejected: drop `IReadStH`, `IIUStH`, `IParamReadStH`, `ICallReadStH`.

## Consequences

### Positive

- Genuine async; cancellation everywhere.
- Connection pooling via `MySqlDataSource` (built-in, no manual pool code).
- Dapper queries map closely to the Java SQL strings — porting is mostly mechanical.
- `record` DTOs catch schema mismatches at compile time with `required` members.

### Negative / trade-offs

- Two dependencies added: `MySqlConnector`, `Dapper`.
- We lose one-line familiarity of `DatabaseFactory.getConnection()` — but gain `await using` in return.
- Transactions become explicit: `using var tx = await conn.BeginTransactionAsync(ct);`.

## Implementation notes

- Packages (M1):
  - `MySqlConnector` (latest stable at time of M1).
  - `Dapper` (latest stable).
- Registration:
  ```csharp
  builder.Services.AddMySqlDataSource(builder.Configuration.GetConnectionString("LoginDb")!);
  ```
- DAO base class: **none**. DAOs are small, each takes the data source via ctor injection.
- Transactions:
  ```csharp
  await using var conn = await ds.OpenConnectionAsync(ct);
  await using var tx = await conn.BeginTransactionAsync(ct);
  await conn.ExecuteAsync(sql1, p1, tx);
  await conn.ExecuteAsync(sql2, p2, tx);
  await tx.CommitAsync(ct);
  ```
- Error handling: `catch (Exception)` only at the process boundary (e.g. a packet handler that logs and returns). DAO methods let exceptions propagate; the caller (controller) decides whether this is a business error or a retry-able failure.

### Schema migrations

See [`data-schema.md`](../data-schema.md). Tool decision: Evolve (SQL files) in M1, can be revisited in M7.

## Affected milestones

- M1: `AddMySqlDataSource`, Dapper dependency, sample DAO (`AccountDao`).
- M2: full Login DAO set.
- M3+: GS, Chat, Game DAOs.

## Related

- ADR-002 (Configuration): connection string lives in `ConnectionStrings:LoginDb`.
- Risk: [R-004](../risks.md).

# M1 — Execution brief

Scope, DoD and smoke scenario live in [`m1-commons-core.md`](m1-commons-core.md). This file is the agent-executable work plan. **Read [`../agent-rules.md`](../agent-rules.md) first.**

## Preconditions

Before touching code, verify:

1. Java reference worktree exists: `ls /tmp/aion-refs/4.6.2/AL-Login/src/com/aionemu/loginserver/controller/AccountController.java`. If missing: `git worktree add /tmp/aion-refs/4.6.2 origin/4.6.2`.
2. Current `.NET` solution builds as-is: `dotnet build AionLightning.NET/AionLightning.NET.sln`. This is the baseline — future changes should never leave the tree non-building.
3. Local MySQL available: `mysql --version`. Create empty `al_server_ls` database.
4. .NET 10 SDK installed: `dotnet --list-sdks | grep 10.0`.

If any precondition fails — stop, surface, do not proceed.

## Reading scope (for agent — see [`../agent-prompt.md`](../agent-prompt.md) tiers)

**Tier A (mandatory):** `agent-rules.md`, `conventions.md`, `m1-commons-core.md`, this file.

**Tier B — must-read ADRs for M1** (open before Step 1):
- [`../adr/001-networking.md`](../adr/001-networking.md) — Pipelines base, used in Step 8.
- [`../adr/002-configuration.md`](../adr/002-configuration.md) — `IOptions<T>` pattern, used in Step 5.
- [`../adr/003-hosting.md`](../adr/003-hosting.md) — `HostApplicationBuilder`, used in Step 6.
- [`../adr/004-database.md`](../adr/004-database.md) — MySqlConnector + Dapper + DbDataSource, used in Step 6 and Step 11.
- [`../adr/007-schema-migration.md`](../adr/007-schema-migration.md) — Evolve, used in Step 7.

**Tier B — step-triggered opens:**
- [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md) — open at Step 8 (networking base).
- [`../data-schema.md`](../data-schema.md) — open at Step 7 (SQL import) and Step 11 (smoke DB check).

**Do NOT read:** `adr/005-callbacks.md`, `adr/006-scripting.md` — not needed in M1 (event bus is only an interface stub; full bus lands in M5).

**Tier C — on-demand (consult only if a concrete question comes up):**
- [`../current-state.md`](../current-state.md) — consult at Step 9 (remove replaced files) if unsure what is currently there.
- [`../risks.md`](../risks.md) — specifically R-001 (ByteBuffer semantics) while writing `PacketReader` in Step 8; R-004 (checked exceptions) during any DAO sketch.
- [`../glossary.md`](../glossary.md) — grep by keyword when a Java idiom is unclear.

## Step order (strict)

Each step ends on a buildable tree and a commit `M1: <step>`.

### Step 1 — `.editorconfig` + solution hygiene

- Create `AionLightning.NET/.editorconfig` with the block from [`../conventions.md`](../conventions.md).
- Update `AionLightning.NET/Directory.Build.props`:
  - Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
  - Add `<Nullable>enable</Nullable>` (already in csproj files — here for inheritance).
  - Keep `DotNetVersion=net10.0`, `ExtensionsVersion=10.0.2`.

Verify: `dotnet build`. Commit.

### Step 2 — Remove dead and rewrite-pending code

Delete (per Rule 1 "commit compiling"), but do it in ONE commit only after replacements land (Step 3+). Reference list:

- `AionLightning.Commons/Callbacks/` — entire directory.
- `AionLightning.Commons/Network/NioServer.cs`
- `AionLightning.Commons/Network/Dispatcher.cs` (removes `Executor` too)
- `AionLightning.Commons/Network/AcceptDispatcherImpl.cs`
- `AionLightning.Commons/Network/AcceptReadWriteDispatcherImpl.cs`
- `AionLightning.Commons/Network/Acceptor.cs`
- `AionLightning.Commons/Network/DisconnectionTask.cs`
- `AionLightning.Commons/Network/DisconnectionThreadPool.cs`
- `AionLightning.Commons/Network/PacketProcessor.cs`
- `AionLightning.Commons/Database/DB.cs`
- `AionLightning.Commons/Database/IReadStH.cs`
- `AionLightning.Commons/Database/IIUStH.cs`
- `AionLightning.Commons/Database/IParamReadStH.cs`
- `AionLightning.Commons/Database/ICallReadStH.cs`
- `AionLightning.Login/Configs/Config.cs`
- `AionLightning.Login/PingPongThread.cs`
- `AionLightning.Commons/TaskManager/AbstractLockManager.cs`
- `AionLightning.Commons/Versioning/*` (after confirming no reference)

**Do not delete yet** — noted, deleted in Step 8 after replacements compile.

### Step 3 — Commons NuGet changes

`AionLightning.Commons.csproj`:

- Remove: `MySql.Data`, `System.Data.SqlClient`.
- Add: `MySqlConnector` (latest stable), `Dapper` (latest stable), `Evolve.Core` (latest stable), `System.IO.Pipelines` (version matching BCL if not transitive).
- Keep: `BouncyCastle.Cryptography` 2.6.2, `Microsoft.CodeAnalysis.CSharp` 5.0.0, `Microsoft.Extensions.Configuration.Json` 10.0.2, `Microsoft.Extensions.Logging` 10.0.2, `Microsoft.Extensions.Logging.Console` 10.0.2, `Quartz` 3.15.1.

Verify: `dotnet restore && dotnet build` — expect build to still pass because deleted code from Step 2 is not yet removed (its build will fail until Step 8). If build fails now, Step 2 constraints were violated — revert.

Commit: `M1: bump Commons packages to MySqlConnector + Dapper + Evolve`.

### Step 4 — Move Ncrypt to Commons

- Create `AionLightning.Commons/Network/Ncrypt/` directory.
- Copy files from `AionLightning.Login/Network/Ncrypt/`:
  - `BlowfishCipher.cs`
  - `CryptEngine.cs`
  - `EncryptedRSAKeyPair.cs`
  - `KeyGen.cs`
- Update namespaces to `AionLightning.Commons.Network.Ncrypt`.
- Update `AionLightning.Login/*` references.
- Verify Java parity: `/tmp/aion-refs/4.6.2/AL-Login/src/com/aionemu/loginserver/network/ncrypt/{BlowfishCipher,CryptEngine,EncryptedRSAKeyPair,KeyGen}.java`.

Verify: `dotnet build`. Commit: `M1: move Ncrypt to Commons`.

### Step 5 — Config records + options registration

Create files under `AionLightning.Commons/Configuration/`:

- `AionOptionsExtensions.cs`:
  ```csharp
  public static class AionOptionsExtensions
  {
      public static IServiceCollection AddAionOptions<T>(
          this IServiceCollection services, string sectionName) where T : class
      {
          services.AddOptions<T>()
              .BindConfiguration(sectionName)
              .ValidateDataAnnotations()
              .ValidateOnStart();
          return services;
      }
  }
  ```

Under `AionLightning.Login/Configs/Options/`:

- `NetworkOptions.cs`, `SecurityOptions.cs`, `AccountsOptions.cs`, `MaintenanceOptions.cs`, `PingPongOptions.cs`, `DatabaseOptions.cs`, `GameServerEntry.cs`.
- Shape example:
  ```csharp
  public sealed record NetworkOptions
  {
      [Required] public required string BindAddress { get; init; }
      [Range(1, 65535)] public int ClientPort { get; init; } = 2106;
      [Range(1, 65535)] public int GameServerPort { get; init; } = 9014;
      public int ReadThreads { get; init; }
      public int WriteThreads { get; init; }
  }
  ```

Rewrite `AionLightning.Login/appsettings.json` into the sectioned layout from [ADR-002](../adr/002-configuration.md). Every value from the flat Java-dotted keys gets mapped to the new section. Replace `Database.Url = jdbc:mysql://...` with `ConnectionStrings.LoginDb = "Server=localhost;Port=3306;..."`.

Delete `AionLightning.Login/Configs/Config.cs`.

Verify: `dotnet build`, service-start throws at boot if a required key is missing (run once without the file — expect `OptionsValidationException`).

Commit: `M1: IOptions<T> records replace static Config singleton`.

### Step 6 — Hosting helper + `MySqlDataSource`

Create `AionLightning.Commons/Hosting/AionHostBuilder.cs`:

```csharp
public static class AionHostBuilder
{
    public static HostApplicationBuilder CreateAion(string[] args, string appName)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSerilog((sp, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(sp)
            .Enrich.FromLogContext());

        if (OperatingSystem.IsWindows())
        {
            builder.Services.AddWindowsService(o => o.ServiceName = appName);
        }

        return builder;
    }
}
```

Create `AionLightning.Commons/Database/AionDataSourceExtensions.cs`:

```csharp
public static class AionDataSourceExtensions
{
    public static IServiceCollection AddAionDataSource(
        this IServiceCollection services,
        IConfiguration config,
        string connectionStringName,
        object? serviceKey = null)
    {
        var cs = config.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' not found");

        var dataSource = new MySqlDataSourceBuilder(cs).Build();

        if (serviceKey is null)
            services.AddSingleton(dataSource);
        else
            services.AddKeyedSingleton(serviceKey, dataSource);

        return services;
    }
}
```

Rewrite `AionLightning.Login/Program.cs`:

```csharp
var builder = AionHostBuilder.CreateAion(args, "AionLogin");

builder.Services
    .AddAionOptions<NetworkOptions>("LoginServer:Network")
    .AddAionOptions<SecurityOptions>("LoginServer:Security")
    .AddAionOptions<AccountsOptions>("LoginServer:Accounts")
    .AddAionOptions<MaintenanceOptions>("LoginServer:Maintenance")
    .AddAionOptions<PingPongOptions>("LoginServer:PingPong");

builder.Services.AddAionDataSource(builder.Configuration, "LoginDb");

builder.Services.AddHostedService<SmokeHost>();

await builder.Build().RunAsync();
```

`SmokeHost` is a temporary `BackgroundService` that validates M1 foundations (gone after M2):

```csharp
public sealed class SmokeHost(
    MySqlDataSource ds,
    IOptions<NetworkOptions> net,
    ILogger<SmokeHost> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        log.LogInformation("M1 smoke: NetworkOptions loaded BindAddress={Bind} ClientPort={Port}",
            net.Value.BindAddress, net.Value.ClientPort);

        await using var conn = await ds.OpenConnectionAsync(ct);
        var result = await conn.QuerySingleAsync<int>("SELECT 1");
        log.LogInformation("M1 smoke: SELECT 1 -> {Result}", result);

        log.LogInformation("M1 smoke: ready. Press Ctrl-C to stop.");

        try { await Task.Delay(Timeout.Infinite, ct); }
        catch (OperationCanceledException) { /* expected */ }
    }
}
```

Commit: `M1: hosting helper + DbDataSource + smoke program`.

### Step 7 — Schema migration wiring

Create `AionLightning.NET/Sql/login/` directory:

```bash
cp /tmp/aion-refs/4.6.2/AL-Login/sql/al_server_ls.sql \
   AionLightning.NET/Sql/login/V1__initial_4_6_2.sql
```

Update `AionLightning.Login.csproj` to copy the SQL folder to output:

```xml
<ItemGroup>
  <None Include="..\Sql\login\**\*.sql" CopyToOutputDirectory="PreserveNewest" LinkBase="Sql/login/" />
</ItemGroup>
```

Wire Evolve before `SmokeHost`:

```csharp
builder.Services.AddHostedService<SchemaMigrationHost>();
```

Where `SchemaMigrationHost` runs Evolve on the `LoginDb` connection string. See [ADR-007](../adr/007-schema-migration.md) for the code pattern. Order is guaranteed by `IHostedService` registration order — `SchemaMigrationHost` before `SmokeHost`.

Verify: with an empty `al_server_ls`, `dotnet run` applies the schema; second run is a no-op.

Commit: `M1: Evolve schema migration on Login startup`.

### Step 8 — Networking base (Pipelines-based)

Create under `AionLightning.Commons/Network/`:

- `PacketReader.cs`, `PacketWriter.cs` per [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md). Include unit-friendly tests in Step 11 later; for now the types must compile and have coverage of `ReadC/H/D/Q/F/S/B`, `WriteC/H/D/Q/F/S/B/Zero`.
- `PacketFormatException.cs`:
  ```csharp
  public sealed class PacketFormatException(string message, Exception? inner = null)
      : InvalidOperationException(message, inner);
  ```
- `AConnection.cs` (new):
  - Async accept → `Socket` instance, wrap in `PipeReader` / `PipeWriter` via either `Socket.ReceiveAsync(Memory<byte>)` loop or `mgravell/Pipelines.Sockets.Unofficial` helper.
  - Frame loop: read 2 bytes → length → read body → `OnPacketAsync(body)` (body = opcode + payload, already decrypted in the subclass after crypto integration in M2).
  - `IAsyncDisposable` with `CancellationToken` through `RunAsync(ct)`.
- `AionPacket.cs`, `AionClientPacket.cs`, `AionServerPacket.cs` per [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md) base classes.
- `IConnectionFactory.cs`:
  ```csharp
  public interface IConnectionFactory<TConnection> where TConnection : AConnection
  {
      TConnection Create(Socket socket, CancellationToken ct);
  }
  ```
- `ServerCfg.cs` (keep, simplify if needed).
- `IPRange.cs` (keep).

### Step 9 — Delete replaced files

Delete the list from Step 2. Verify `dotnet build` green. Commit: `M1: drop legacy NIO port + static Config + sync DB`.

### Step 10 — Event-bus interfaces (no implementation)

Under `AionLightning.Commons/Events/`:

- `IGameEvent.cs`:
  ```csharp
  public interface IGameEvent;
  ```
- `IEventBus.cs`, `IEventHandler.cs` per [ADR-005](../adr/005-callbacks.md).

No implementation yet (lands in M5). `IEventBus` must not be injected by any M1 code.

Commit: `M1: event bus contracts`.

### Step 11 — Smoke run

1. Start local MySQL, create empty database `al_server_ls`.
2. Edit `appsettings.Development.json` (create; gitignored) with real connection string.
3. `dotnet run --project AionLightning.Login`.
4. Expected log sequence:
   - `Evolve: applied V1__initial_4_6_2`
   - `M1 smoke: NetworkOptions loaded BindAddress=0.0.0.0 ClientPort=2106`
   - `M1 smoke: SELECT 1 -> 1`
   - `M1 smoke: ready. Press Ctrl-C to stop.`
5. `Ctrl-C` → within ~2 s, process exits cleanly.
6. Re-run: Evolve reports "no migrations to apply".
7. Tamper: remove `LoginServer:Network:BindAddress` → expect `OptionsValidationException` at startup.

Commit: `M1: pass smoke`.

## Exit criteria (M1 done)

- All DoD items in [`m1-commons-core.md`](m1-commons-core.md) checked.
- `checklist.md` M1 section ticks through.
- Journal entry added in [`../journal.md`](../journal.md): `M1 closed`.
- Move active milestone pointer in [`../checklist.md`](../checklist.md) to M2.

## References

- Java: `/tmp/aion-refs/4.6.2/AL-Commons/src/com/aionemu/commons/network/*.java` — for the deleted NIO port (reference only).
- Java: `/tmp/aion-refs/4.6.2/AL-Login/sql/al_server_ls.sql` — schema source.
- Java: `/tmp/aion-refs/4.6.2/AL-Login/src/com/aionemu/loginserver/network/ncrypt/*.java` — Ncrypt logic source.
- ADRs: [001](../adr/001-networking.md), [002](../adr/002-configuration.md), [003](../adr/003-hosting.md), [004](../adr/004-database.md), [007](../adr/007-schema-migration.md).
- Packet adapter: [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md).

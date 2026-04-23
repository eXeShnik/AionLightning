# ADR-002: Configuration

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

The Java original uses `.properties` files loaded through a custom `Config` class with reflection-based attribute (`@Property`) binding.

The current .NET port in `AionLightning.Login/Configs/Config.cs` follows the same shape but as a `public static class`:

```csharp
public static class Config
{
    public static string LOGIN_BIND_ADDRESS { get; private set; }
    public static int LOGIN_PORT { get; private set; }
    // ... ~20 more static properties
    public static void Load() { /* read JSON, assign statics */ }
}
```

Problems:

- Static state — every service that reads config is tightly coupled to a global.
- No validation; wrong key types fail at first access, not at startup.
- Hard to unit test — you cannot construct a service with alternate config without mutating process-global state.
- Does not participate in `IOptionsMonitor<T>` reload semantics.

## Decision

Use the **.NET options pattern** with `record` DTOs and `ValidateOnStart`:

```csharp
public sealed record NetworkOptions
{
    [Required] public required string BindAddress { get; init; }
    [Range(1, 65535)] public int ClientPort { get; init; }
    [Range(1, 65535)] public int GameServerPort { get; init; }
    public int ReadThreads { get; init; }
    public int WriteThreads { get; init; }
}
```

Registration:

```csharp
builder.Services
    .AddOptions<NetworkOptions>()
    .BindConfiguration("LoginServer:Network")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Consumption:

```csharp
public sealed class NetConnector(IOptions<NetworkOptions> options, ...)
{
    private readonly NetworkOptions _net = options.Value;
    // ...
}
```

- **`IOptions<T>`** — singleton, default choice.
- **`IOptionsSnapshot<T>`** — per-scope reload. Not used here (no HTTP request scope).
- **`IOptionsMonitor<T>`** — singleton with change notifications. Used only where hot reload makes sense (e.g. `FolderListenerService` script paths).

## Alternatives considered

### Alt-1: Keep the static `Config` class

- Pros: no porting cost.
- Cons: breaks DI, no validation, global state.
- Why rejected: costs more in testability than the port saves.

### Alt-2: Direct `IConfiguration` reads everywhere

- Pros: zero binding code.
- Cons: no typing, no validation, stringly-typed access scattered across the code.
- Why rejected: same readability problems as Java `.properties` + more noise.

### Alt-3: Custom attribute-based binding (mirror Java `@Property`)

- Pros: exactly mirrors the Java pattern.
- Cons: reinvents `BindConfiguration` + `DataAnnotations`.
- Why rejected: reinventing the wheel.

## Consequences

### Positive

- Fails fast on misconfiguration (`ValidateOnStart`).
- Every service declares its config dependency through the constructor — easy to mock.
- `record` DTOs are immutable by default; no accidental runtime mutation.

### Negative / trade-offs

- `appsettings.json` key layout changes from flat Java-dotted keys (`loginserver.network.client.port`) to sectioned JSON. We have two choices:
  1. Keep the flat keys and bind via explicit `IConfiguration.GetValue<int>(...)` per field.
  2. Restructure into sections.
- Decision: **restructure into sections**. Flat dotted keys were a Java-properties legacy; JSON sections read better and map to records directly. The `appsettings.json` file will be rewritten in M1, but individual values stay the same.

## Implementation notes

### `appsettings.json` shape (target, M1)

```json
{
  "LoginServer": {
    "Network": {
      "BindAddress": "0.0.0.0",
      "ClientPort": 2106,
      "GameServerPort": 9014,
      "ReadThreads": 0,
      "WriteThreads": 0
    },
    "Security": {
      "EnableFloodProtection": true,
      "EnableBruteforceProtection": true,
      "LoginTriesBeforeBan": 5,
      "BanTimeForBruteforcing": 15,
      "ExcludedIps": ""
    },
    "Accounts": {
      "Charset": "ISO8859_2",
      "AutoCreate": true,
      "FastReconnectionTime": 10
    },
    "Maintenance": {
      "Enabled": false,
      "GmLevel": 3
    },
    "PingPong": {
      "Enabled": true,
      "DelayMs": 3000
    }
  },
  "ConnectionStrings": {
    "LoginDb": "Server=localhost;Port=3306;Database=al_server_ls;Uid=root;Pwd=;"
  },
  "Serilog": { /* ... */ }
}
```

### File removals

- `AionLightning.Login/Configs/Config.cs` — deleted.
- `AionLightning.Login/Configs/SvStatsConfig.cs` — reviewed, likely replaced by a record.

## Affected milestones

- M1: introduces the option records and binding helpers.
- M2: consumers in Login use `IOptions<T>`.
- M3–M6: every new server has its own root record (`ChatOptions`, `GameOptions`).

## Related

- ADR-003 (Hosting) — the builder registers options on startup.
- Risk: none specific.

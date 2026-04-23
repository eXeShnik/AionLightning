# ADR-003: Hosting

- **Status:** Accepted
- **Date:** 2026-04-23

## Context

Four processes (Commons is a library, the other three are servers) need a consistent hosting model:

- Long-running server with a single main listener loop.
- Structured logging, config, DI, graceful shutdown.
- Support for Windows service mode (Java used to run under Wrapper; the .NET equivalent is `UseWindowsService`).

The current .NET port uses `Host.CreateDefaultBuilder(args)` + `UseSerilog(...)` + `ConfigureServices(...)` + `AddHostedService<LoginServer>()`. Functional but not idiomatic for new .NET 10 code — Microsoft's Worker Service template now uses `Host.CreateApplicationBuilder`.

## Decision

Use **`Host.CreateApplicationBuilder(args)`** with the flat property-style API, and a single `BackgroundService` per server process.

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<NetworkOptions>(builder.Configuration.GetSection("LoginServer:Network"));
builder.Services
    .AddOptions<NetworkOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSerilog((sp, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(sp));

builder.Services.AddMySqlDataSource(builder.Configuration.GetConnectionString("LoginDb")!);
builder.Services.AddSingleton<NetConnector>();
builder.Services.AddHostedService<LoginServerHost>();

await builder.Build().RunAsync();
```

- `BackgroundService` (not `IHostedService`) — it gives us an `ExecuteAsync(CancellationToken)` contract with built-in cancellation propagation.
- Server owns its listener(s) inside `ExecuteAsync`.
- `UseWindowsService()` extension added conditionally for Login / Chat / Game on Windows.

## Alternatives considered

### Alt-1: Keep `Host.CreateDefaultBuilder`

- Pros: already in place.
- Cons: Microsoft marks it as legacy for new non-web processes. Verbose fluent API vs. flat properties.
- Why rejected: low migration cost, long-term clarity win.

### Alt-2: `WebApplication.CreateBuilder`

- Pros: same flat API, includes routing / middleware.
- Cons: drags the entire HTTP server and routing surface into apps that do not serve HTTP.
- Why rejected: unused surface is a maintenance liability.

### Alt-3: Custom main loop without Generic Host

- Pros: minimum dependencies.
- Cons: reinvents graceful shutdown, configuration, logging and DI wiring.
- Why rejected: for nothing.

## Consequences

### Positive

- Same hosting shape in all four apps — easy to read and extend.
- `CancellationToken` propagation from the host lifetime is wired in by default.
- Health checks, metrics, OpenTelemetry integrations are one `Add...` call away if ever needed.

### Negative / trade-offs

- Requires rewriting the current `LoginServer.cs` (`IHostedService`) into `BackgroundService`. The commented-out initialisation block in `LoginServer.OnStarted` becomes concrete `ExecuteAsync` code.

## Implementation notes

- .NET version: `net10.0` (already pinned in `Directory.Build.props`).
- Package: nothing extra — `Microsoft.Extensions.Hosting` 10.0.2 is already referenced.
- Windows service: `Microsoft.Extensions.Hosting.WindowsServices` is referenced by Login; use `builder.Services.AddWindowsService(o => o.ServiceName = "AionLogin");` conditionally.

### Shared helper

Put a small extension method in `Commons`:

```csharp
public static class AionHostBuilder
{
    public static HostApplicationBuilder CreateAion(string[] args, string appName)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSerilog((sp, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(sp));
        if (OperatingSystem.IsWindows())
        {
            builder.Services.AddWindowsService(o => o.ServiceName = appName);
        }
        return builder;
    }
}
```

Each server starts as:

```csharp
var builder = AionHostBuilder.CreateAion(args, "AionLogin");
// ... AddOptions, AddSingleton, AddHostedService ...
await builder.Build().RunAsync();
```

## Affected milestones

- M1: introduces the helper + migrates Login from `CreateDefaultBuilder` to `CreateApplicationBuilder`.
- M2–M6: every server uses the helper.

## Related

- ADR-001 (Networking): listeners run inside the `BackgroundService`.
- ADR-002 (Configuration): registered via the same builder.

# AionLightning Migration Plan

## Project Goals
- Rewrite the 4 modules (Commons, Login, Chat, Game) to .NET 10 console apps.
- Use .NET Host builder for application lifecycle management.
- Use `IConfiguration` with JSON files for configuration.
- Use Dependency Injection for services.

## Architectural Decisions
- **Logging**: Use `Microsoft.Extensions.Logging.ILogger<T>` injected via constructors instead of a static logger factory.
- **Cryptography**: Use the `BouncyCastle.Cryptography` library for all cryptographic operations.
- **Database**: Use `MySql.Data` for MySQL database connectivity.
- **Networking**: Port the custom Java NIO networking layer to a .NET equivalent using `System.Net.Sockets`.
- **AOP/Callbacks**: The Java Agent-based AOP for callbacks will be provisionally migrated with placeholder classes. A full implementation will require a dedicated .NET AOP library like Castle.Core or source generators.

## Migration Steps
1.  [✓] Set up solution and project structure for .NET 10.
2.  [✓] Implement `ILogger` with console output for all projects.
3.  [✓] Refactor logging to use constructor injection.
4.  [✓] Add `BouncyCastle.Cryptography` package.
5.  [In-Progress] Migrate `AL-Commons` module.
    - [✓] `utils`
    - [✓] `callbacks`
    - [✓] `configuration`
    - [✓] `database`
    - [✓] `network`
    - [✓] `objects`
    - [✓] `options`
    - [✓] `scripting`
    - [ ] `services`
    - [ ] `taskmanager`
    - [ ] `versionning`
6.  [ ] Migrate `AL-Login` module.
7.  [ ] Migrate `AL-Chat` module.
8.  [ ] Migrate `AL-Game` module.
9.  [ ] Implement a .NET AOP solution for the callbacks system.
10. [ ] Integrate the custom `ConfigurableProcessor` with .NET's `IConfiguration`.

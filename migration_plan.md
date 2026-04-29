# AionLightning Migration Plan

## **Current repo based has many server versions, our migration based on 4.6.0**

## **Main thing I want u to save your history, all important doings or things**

## Before implementing u should check plan, then split it by smaller tasks and save near project, then assign them to another agents

## Java project structure

- `Tools` folder we can skip
- Project has been written with old Java 1.7, so I think some libs and classes are unnecessary for latest net 10, so u should analyses migrated code, find them then save it as important thing, so u will know later what we do not need to rewrite and use it from net 10
- Java project folder we need to care about them - `src/` and `libs/`, `data/`
- `AL-Commons` - shared lib, here we have common classes which uses in other projects
- `AL-Chat` - console app, uses for chat packets, communicating with client and game server
- `AL-Login` - console app, uses for waiting login packets from game then make connection from client to game server
- `AL-Game` - console app, uses for main game packets, working with skills, quest, other things related to game

## Project Goals

- Rewrite the 4 modules (Commons - Shared, Login, Chat, Game) to .NET 10 console apps.
- Use .NET Host builder for application lifecycle management.

## Architectural Decisions

- **Configuration** Use `IConfiguration` with JSON files for configuration.
- **Application architecture** Use Dependency Injection for services.
- **Logging**: Use `Microsoft.Extensions.Logging.ILogger<T>` injected via constructors instead of a static logger factory. For logging we use `Serilog`
- **Cryptography**: Use the `BouncyCastle.Cryptography` library for all cryptographic operations.
- **Database**: Use `MySql.Data` for MySQL database connectivity.
- **Networking**: Port the custom Java NIO networking layer to a .NET equivalent using `System.Net.Sockets.TcpClient`.
- **AOP/Callbacks**: The Java Agent-based AOP for callbacks will be provisionally migrated with placeholder classes. A full implementation will require a dedicated .NET AOP library like Castle.Core or source generators. ??? I think we need to rethink this part, we might not need it

## Rework current migrated things

- First of all before moving forward, we need to replace simple `Socket` implementation with `TcpClient` and `TcpListener`
- Regarding scripting I added `CSharpCompilerService` what I am thinking about that, we need to listen `Scripts` folder in corresponding app (Login, Game, Chat), I added `FolderListenerService` to listen folder changes, so we can listen then load and cache script into memory, so important thing that all scripts will be use only existing classes which we have in Common or any others project

## Migration Steps

1. [✓] Set up solution and project structure for .NET 10.
2. [✓] Implement `ILogger` with console output for all projects.
3. [✓] Refactor logging to use constructor injection.
4. [✓] Add `BouncyCastle.Cryptography` package.
5. [In-Progress] Migrate `AL-Commons` module.
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
6. [✓] Migrate `AL-Login` module.
   - [✓] Network layer (TcpListener/TcpClient), packet framing, blowfish+RSA crypto
   - [✓] Auth flow: CM_LOGIN, CM_PLAY, CM_SERVER_LIST, CM_AUTH_GG, CM_UPDATE_SESSION
   - [✓] DAO layer: AccountDao, BannedIpDao backed by MySql.Data
   - [✓] GameServer registry: GameServerTable, GameServerInfo, GS↔LS connection
   - [✓] DI wiring in Program.cs; appsettings.json configuration
7. [✓] Migrate `AL-Chat` module. (M4 — 2026-04-29)
   - [✓] Chat server project setup (csproj, appsettings.json, options)
   - [✓] GS↔Chat listener (port 9021): CM_CS_AUTH, CM_PLAYER_AUTH, CM_PLAYER_LOGOUT, CM_PLAYER_GAG
   - [✓] Aion client↔Chat listener (port 10241): CM_CHAT_INI, CM_PLAYER_AUTH, CM_CHANNEL_REQUEST, CM_CHANNEL_MESSAGE
   - [✓] ChatService: token generation (SHA-256), dynamic channel registry, rate-limiting, gag
   - [✓] SM_GS_AUTH_RESPONSE, SM_PLAYER_AUTH_RESPONSE, SM_CHANNEL_RESPONSE, SM_CHANNEL_MESSAGE
   - [✓] GS-side CS connection (CsConnection, CsConnectionHolder, reconnect loop in GameServerHost)
   - [✓] CM_CHAT_AUTH (0x14C) in GsPacketHandlerFactory; SM_CHAT_INIT (0xE6) to Aion client
   - [✓] Player logout notification from GsClientConnection.DisposeAsync
8. [In-Progress] Migrate `AL-Game` module.
   - [✓] Player model, DAO layer (player + appearance), movement + login packet handlers
   - [✓] World registry, event bus, scripting, LS/CS connections
   - [✓] Config options: WorldOptions, RateOptions, GsOptions (IOptions<T> DI pattern)
   - [✓] DataManager + XML loading infrastructure (IDataManager DI singleton)
   - [✓] PlayerStatsData — loads per-class XML templates from data/static_data/stats/player/, post-load math (HP/MP/evasion/block/parry recalc)
   - [✓] PlayerInitialData — loads spawn locations from player_initial_data.xml
   - [✓] PlayerStatsTemplate + StatsTemplate + CreatureSpeeds model hierarchy
   - [✓] CM_ENTER_WORLD wired to DataManager — real HP/MP from class+level template
   - [✓] SM_STATS_INFO uses real attributes (power/health/agility/accuracy/knowledge/will, evasion/block/parry, combat stats) from template
   - [ ] Skill data + PlayerSkillList (template lookup + basic cooldown)
   - [ ] NPC template + basic spawning (SpawnTemplate → Npc in World, no AI)
   - [ ] PlayerExperienceTable, XP gain, level-up handling
   - [ ] Item system, inventory model, starting items from PlayerInitialData

# Migration checklist

Single source of truth for status. Details live in the matching `milestones/mN-*.md`.

**Legend:** `[ ]` — not started · `[~]` — in progress · `[x]` — done · `[-]` — deferred / cancelled.

Current active milestone: **M5** (code complete; smoke tests deferred — needs MySQL + Aion 4.6.0 client).

---

## M1 — Commons Core MVP
Status: `[~]` in progress · Blocks: M2 · ADRs: 001, 002, 003, 004

### DoD
- [x] `AionLightning.NET.sln` builds with zero warnings in strict mode
- [x] No `NotImplementedException` in the public API of any subsystem in M1 scope
- [ ] Smoke program (minimal Login host): starts, reads config, opens a DB connection, runs `SELECT 1`, stops cleanly on Ctrl-C with `CancellationToken` propagated — **deferred: needs local MySQL (run before M2 DoD)**
- [x] `Directory.Build.props` updated as required by [ADR-003](adr/003-hosting.md)

### Scope
- [x] Hosting: `Host.CreateApplicationBuilder` helper, shared base for all servers
- [x] Config: `record` DTOs + `BindConfiguration` + `ValidateOnStart` ([ADR-002](adr/002-configuration.md))
- [x] Logging: Serilog driven by `IConfiguration` (keep the current mechanism)
- [x] DB: `DbDataSource` singleton + MySqlConnector + Dapper ([ADR-004](adr/004-database.md))
- [x] Networking base: async `AConnection`, `AionPacket` / `AionClientPacket` / `AionServerPacket`, framing via `SequenceReader<byte>` ([ADR-001](adr/001-networking.md))
- [x] Crypto: move `Login/Network/Ncrypt/*` into `Commons/Network/Ncrypt/`
- [x] Utils kept: `Rnd`, `MTRandom`, `Base64`, `NetworkUtils`, `ClassUtils`

### Remove
- [x] `Commons/Network/NioServer.cs`
- [x] `Commons/Network/Dispatcher.cs`
- [x] `Commons/Network/AcceptDispatcherImpl.cs`
- [x] `Commons/Network/AcceptReadWriteDispatcherImpl.cs`
- [x] `Commons/Network/Executor.cs` (the class at the bottom of `Dispatcher.cs`)
- [x] `Commons/Database/DB.cs` (rewrite)
- [x] `Commons/Database/IReadStH.cs`, `IIUStH.cs`, `IParamReadStH.cs`, `ICallReadStH.cs`
- [x] `Commons/Callbacks/*` ([ADR-005](adr/005-callbacks.md))
- [x] `Login/Configs/Config.cs` (static singleton)

Scope: [`milestones/m1-commons-core.md`](milestones/m1-commons-core.md) · **Execution brief: [`milestones/m1-execution-brief.md`](milestones/m1-execution-brief.md)** (read first)

---

## M2 — Login server: client auth path
Status: `[~]` in progress · Requires: M1 · ADRs: 001, 002, 003, 004

### DoD
- [ ] Aion 4.6.0 client connects to Login (port 2106) and reaches the serverlist screen
- [ ] An existing account from the DB logs in successfully
- [ ] Wrong password → the client shows the correct message
- [ ] Banned IP → the client receives a ban response
- [ ] Serverlist contains a single hard-coded stub (no real GS yet)
- [ ] `Ctrl-C` gracefully closes all connections and the listener

### Scope
- [x] Client listener on port 2106 using Pipelines (`LoginServerHost`)
- [x] Packet pipeline: accept → Blowfish → decode → handle → encode → send (`LoginConnection`)
- [x] Full set of Java `clientpackets/`: `CM_AUTH_GG`, `CM_LOGIN`, `CM_SERVER_LIST`, `CM_PLAY`, `CM_UPDATE_SESSION`
- [x] Full set of Java `serverpackets/`: `SM_INIT`, `SM_AUTH_GG`, `SM_LOGIN_OK`, `SM_LOGIN_FAIL`, `SM_SERVER_LIST`, `SM_PLAY_OK`, `SM_PLAY_FAIL`, `SM_UPDATE_SESSION`
- [x] DAOs (Dapper): `IAccountDao`/`AccountDaoImpl`, `IBannedIpDao`/`BannedIpDaoImpl`
- [x] Controllers: `IAccountController`/`AccountController`, `BannedIpController` (async)
- [x] `GameServerTable` seeded from `appsettings.json` `LoginServer:GameServers[]` via `LoadFromConfig`
- [x] `SessionKey` generation for handoff

Out of scope: GS listener, inter-server protocol, auto-create accounts (the config key stays, but only a log entry is produced).

Scope: [`milestones/m2-login-auth.md`](milestones/m2-login-auth.md) · **Execution brief: [`milestones/m2-execution-brief.md`](milestones/m2-execution-brief.md)** (packet inventory, handshake diagram, port order — read first)

---

## M3 — Login ↔ GameServer handshake
Status: `[~]` code complete, smoke test pending · Requires: M2 · ADRs: 001, 002, 003, 004

### DoD
- [~] Second Login listener on port 9014 accepts GS connections (coded; smoke deferred)
- [~] `.NET Login` + `.NET GameServer skeleton` complete the handshake (coded; smoke deferred)
- [~] After picking a server, the client switches to the GS socket and GS accepts the connection with a valid `SessionKey` (coded; smoke deferred)
- [~] Shutting down GS → Login detects the disconnect and updates the serverlist status (coded; smoke deferred)

### Scope
- [x] LS-side `GsConnection`, `GsConnectionFactory`, GS `clientpackets/` + `serverpackets/`
- [x] GS-side: port the Java side of the protocol from `AL-Game/src/com/aionemu/gameserver/network/loginserver/`
- [x] Account handoff: LS issues `SessionKey`, GS validates it on connect
- [x] Ping-pong (`PingPongThread.cs` → async `PeriodicTimer`)
- [x] GS-side minimal `GameServerHost` `BackgroundService` that accepts client connections (no world logic yet)

Out of scope: character select, world entry, any game logic.

Scope: [`milestones/m3-login-gs-handshake.md`](milestones/m3-login-gs-handshake.md) · **Execution brief: [`milestones/m3-execution-brief.md`](milestones/m3-execution-brief.md)**

---

## M4 — Chat server
Status: `[ ]` not started · Requires: M3 · ADRs: 001, 002, 003, 004

### DoD
- [ ] `.NET Chat` boots and accepts GS registration
- [ ] `/shout` from one character reaches another through Chat (smoke with two clients)
- [ ] `/whisper`, party, group and legion chat — base packets working

### Scope
- [ ] Chat↔GS listener + client
- [ ] Chat client listener (client 4.6.0 → Chat server port)
- [ ] Core chat packets (shout, whisper, party, group, legion)
- [ ] Chat DAOs (if needed — banned words, mute history)

Scope: [`milestones/m4-chat-server.md`](milestones/m4-chat-server.md) · **Execution brief: [`milestones/m4-execution-brief.md`](milestones/m4-execution-brief.md)**

---

## M5 — Game: world entry
Status: `[~]` in progress · Requires: M3 · ADRs: 001..006

### DoD
- [~] Character select screen works after the handoff from Login (packets coded; smoke deferred)
- [~] Client enters the world, sees its character at the starting location (coded; smoke deferred)
- [~] Movement (WASD) is broadcast to other clients (coded; smoke deferred)
- [x] Logout / exit saves state to the DB correctly (position + online=false on DisposeAsync)
- [~] A second session sees the first session at the same location (coded; smoke deferred)

### Scope
- [x] `GameServer` `BackgroundService` — `GameServerHost` with LS reconnect + Aion listener (M3)
- [x] Core entities: `Player`, `Creature`, `VisibleObject`, `Position`
- [ ] World grid / region system (simplified)
- [x] Character select packets: `CM_VERSION_CHECK`, `CM_CHARACTER_LIST`, `CM_MAY_LOGIN_INTO_GAME`, `CM_QUIT`, `CM_PING`, `CM_TIME_CHECK`, `CM_MAC_ADDRESS`; server packets `SM_VERSION_CHECK`, `SM_CHARACTER_LIST`, `SM_MAY_LOGIN_INTO_GAME`, `SM_QUIT_RESPONSE`, `SM_PONG`, `SM_TIME_CHECK`
- [x] World entry packets: `CM_ENTER_WORLD`, `CM_LEVEL_READY`, `CM_MOVE`, `SM_PLAYER_SPAWN`, `SM_MOVE`; plus `SM_CHARACTER_SELECT`, `SM_PLAYER_INFO`, `SM_STATS_INFO`, `SM_GAME_TIME`
- [x] DAOs: `IPlayerDao`/`PlayerDaoImpl`, `IPlayerAppearanceDao`/`PlayerAppearanceDaoImpl`
- [-] `InventoryDao` (minimum) — deferred; empty inventory sent via WriteH(0) in SM_CHARACTER_LIST; enter-world does not send SM_INVENTORY_INFO
- [x] Event bus (`InMemoryEventBus`) — first use ([ADR-005](adr/005-callbacks.md))
- [x] Scripting service rewrite — `ScriptService` with collectible ALC, `CSharpCompilerService` fixed, `IScript`/`IScriptHost` contracts, sample `Greeter.cs` ([ADR-006](adr/006-scripting.md))
- [x] Game DB schema migration (`V1__initial_game.sql`: `players` + `player_appearance`)

Out of scope: AI, skills, combat, quests, items beyond appearance, trade, PvP.

Scope: [`milestones/m5-game-world-entry.md`](milestones/m5-game-world-entry.md) · **Execution brief: [`milestones/m5-execution-brief.md`](milestones/m5-execution-brief.md)**

---

## M6 — Game content expansion
Status: `[ ]` not started · Requires: M5

Split into sub-milestones (order is a draft; revisit before starting each):

### M6.1 — AI & combat base
- [ ] NPC / monster spawns
- [ ] Basic AI (idle, aggro, chase, return)
- [ ] Combat formulas, HP / MP

### M6.2 — Skills
- [ ] Skill DAO + data-files reader
- [ ] Skill cast pipeline
- [ ] Effects system on top of the event bus

### M6.3 — Items & inventory
- [ ] Item DAO, inventory slots
- [ ] Equip / unequip, stat recalculation
- [ ] Cube / warehouse

### M6.4 — Quests
- [ ] Quest engine (scripting-heavy, see ADR-006)
- [ ] Quest DAO

### M6.5 — Instances / dungeons
- [ ] Instance manager, enter / leave
- [ ] Per-instance state

### M6.6 — Trade / broker / auction
- [ ] Direct trade
- [ ] Broker / auction house

### M6.7 — Legions / alliances / groups
- [ ] Group formation, loot rules
- [ ] Legion storage, permissions

### M6.8 — PvP / Abyss
- [ ] Faction mechanics
- [ ] Siege / Abyss points

Scope: [`milestones/m6-game-content.md`](milestones/m6-game-content.md) · **Execution brief (hub): [`milestones/m6-execution-brief.md`](milestones/m6-execution-brief.md)** · sub-milestone briefs are spun up at each sub-M start following the universal procedure in the hub.

---

## M7 — Verification harness
Status: `[ ]` not started · Requires: the core scenarios from M6 to be working

### DoD
- [ ] Unit tests for codec (packet serialize/deserialize) with golden `byte[]`
- [ ] Integration tests for DAOs using Testcontainers + MySQL
- [ ] Packet replay: golden traces captured from the Java server
- [ ] CI: a pipeline runs build + test on every PR
- [ ] `migration/README.md` updated with test instructions

Scope: [`milestones/m7-verification-harness.md`](milestones/m7-verification-harness.md) · **Execution brief: [`milestones/m7-execution-brief.md`](milestones/m7-execution-brief.md)**

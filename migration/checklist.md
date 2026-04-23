# Migration checklist

Single source of truth for status. Details live in the matching `milestones/mN-*.md`.

**Legend:** `[ ]` — not started · `[~]` — in progress · `[x]` — done · `[-]` — deferred / cancelled.

Current active milestone: **M1**.

---

## M1 — Commons Core MVP
Status: `[ ]` not started · Blocks: M2 · ADRs: 001, 002, 003, 004

### DoD
- [ ] `AionLightning.NET.sln` builds with zero warnings in strict mode
- [ ] No `NotImplementedException` in the public API of any subsystem in M1 scope
- [ ] Smoke program (minimal Login host): starts, reads config, opens a DB connection, runs `SELECT 1`, stops cleanly on Ctrl-C with `CancellationToken` propagated
- [ ] `Directory.Build.props` updated as required by [ADR-003](adr/003-hosting.md)

### Scope
- [ ] Hosting: `Host.CreateApplicationBuilder` helper, shared base for all servers
- [ ] Config: `record` DTOs + `BindConfiguration` + `ValidateOnStart` ([ADR-002](adr/002-configuration.md))
- [ ] Logging: Serilog driven by `IConfiguration` (keep the current mechanism)
- [ ] DB: `DbDataSource` singleton + MySqlConnector + Dapper ([ADR-004](adr/004-database.md))
- [ ] Networking base: async `AConnection`, `AionPacket` / `AionClientPacket` / `AionServerPacket`, framing via `SequenceReader<byte>` ([ADR-001](adr/001-networking.md))
- [ ] Crypto: move `Login/Network/Ncrypt/*` into `Commons/Network/Ncrypt/`
- [ ] Utils kept: `Rnd`, `MTRandom`, `Base64`, `NetworkUtils`, `ClassUtils`

### Remove
- [ ] `Commons/Network/NioServer.cs`
- [ ] `Commons/Network/Dispatcher.cs`
- [ ] `Commons/Network/AcceptDispatcherImpl.cs`
- [ ] `Commons/Network/AcceptReadWriteDispatcherImpl.cs`
- [ ] `Commons/Network/Executor.cs` (the class at the bottom of `Dispatcher.cs`)
- [ ] `Commons/Database/DB.cs` (rewrite)
- [ ] `Commons/Database/IReadStH.cs`, `IIUStH.cs`, `IParamReadStH.cs`, `ICallReadStH.cs`
- [ ] `Commons/Callbacks/*` ([ADR-005](adr/005-callbacks.md))
- [ ] `Login/Configs/Config.cs` (static singleton)

Scope: [`milestones/m1-commons-core.md`](milestones/m1-commons-core.md) · **Execution brief: [`milestones/m1-execution-brief.md`](milestones/m1-execution-brief.md)** (read first)

---

## M2 — Login server: client auth path
Status: `[ ]` not started · Requires: M1 · ADRs: 001, 002, 003, 004

### DoD
- [ ] Aion 4.6.0 client connects to Login (port 2106) and reaches the serverlist screen
- [ ] An existing account from the DB logs in successfully
- [ ] Wrong password → the client shows the correct message
- [ ] Banned IP → the client receives a ban response
- [ ] Serverlist contains a single hard-coded stub (no real GS yet)
- [ ] `Ctrl-C` gracefully closes all connections and the listener

### Scope
- [ ] Client listener on port 2106 using Pipelines
- [ ] Packet pipeline: accept → Blowfish → decode → handle → encode → send
- [ ] Full set of Java `clientpackets/` from `AL-Login/src/com/aionemu/loginserver/network/aion/clientpackets/`
- [ ] Full set of Java `serverpackets/` from the same package
- [ ] DAOs (Dapper): `AccountDao`, `AccountTimeDao`, `BannedIpDao`, `BannedMacDao`, `PremiumDao`
- [ ] Controllers: `AccountController`, `BannedIpController`, `BannedMacManager`
- [ ] Static `GameServerTable` replaced by a config-driven `record[]` in `appsettings.json`
- [ ] `SessionKey` generation for handoff

Out of scope: GS listener, inter-server protocol, auto-create accounts (the config key stays, but only a log entry is produced).

Scope: [`milestones/m2-login-auth.md`](milestones/m2-login-auth.md) · **Execution brief: [`milestones/m2-execution-brief.md`](milestones/m2-execution-brief.md)** (packet inventory, handshake diagram, port order — read first)

---

## M3 — Login ↔ GameServer handshake
Status: `[ ]` not started · Requires: M2 · ADRs: 001, 002, 003, 004

### DoD
- [ ] Second Login listener on port 9014 accepts GS connections
- [ ] `.NET Login` + `.NET GameServer skeleton` complete the handshake (auth + registration)
- [ ] After picking a server, the client switches to the GS socket and GS accepts the connection with a valid `SessionKey`
- [ ] Shutting down GS → Login detects the disconnect and updates the serverlist status

### Scope
- [ ] LS-side `GsConnection`, `GsConnectionFactory`, GS `clientpackets/` + `serverpackets/`
- [ ] GS-side: port the Java side of the protocol from `AL-Game/src/com/aionemu/gameserver/network/loginserver/`
- [ ] Account handoff: LS issues `SessionKey`, GS validates it on connect
- [ ] Ping-pong (`PingPongThread.cs` → async `PeriodicTimer`)
- [ ] GS-side minimal `GameServer.cs` `BackgroundService` that accepts client connections (no world logic yet)

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
Status: `[ ]` not started · Requires: M3 · ADRs: 001..006

### DoD
- [ ] Character select screen works after the handoff from Login
- [ ] Client enters the world, sees its character at the starting location
- [ ] Movement (WASD) is broadcast to other clients
- [ ] Logout / exit saves state to the DB correctly
- [ ] A second session sees the first session at the same location

### Scope
- [ ] `GameServer` `BackgroundService` (extends the M3 skeleton)
- [ ] Core entities: `Player`, `Creature`, `VisibleObject`, `Position`
- [ ] World grid / region system (simplified)
- [ ] Character select + world entry packets
- [ ] DAOs: `PlayerDao`, `InventoryDao` (minimum), `PlayerAppearanceDao`
- [ ] Event bus (`Channel<T>`) — first use ([ADR-005](adr/005-callbacks.md))
- [ ] Scripting service — full implementation ([ADR-006](adr/006-scripting.md))

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

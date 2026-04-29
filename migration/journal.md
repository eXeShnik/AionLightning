# Journal

Chronological log of key decisions about the migration. Newest entries on top. Format: date → short decision → links to ADR / milestone / risk.

---

## 2026-04-28 · M1 closed (code complete, smoke deferred)

- **M1 code complete.** All deleted/replaced files committed in `bfe1cc19`. Solution builds 0 errors / 0 warnings with `TreatWarningsAsErrors`. Commit: `M1: drop legacy NIO port + static Config + sync DB`.
- **Key decisions during M1:**
  - `EvolveDb.Evolve` namespace (v3.x renamed from `Evolve.Evolve` in v2.x) — fixed in `SchemaMigrationHost.cs`.
  - Circular namespace `Account` ↔ `GameServerInfo` resolved via `global::AionLightning.Login.GameServerInfo?` qualifier in `Model/Account.cs`.
  - `LoginState` enum kept ALL_CAPS (`CONNECTED`, `AUTHED_GG`) to match `AionPacketHandlerFactory` switch arms.
  - Ncrypt moved from `Login/Network/Ncrypt/` to `Commons/Network/Ncrypt/` per ADR-001.
  - Callbacks (`DotNetAgentEnhancer` etc.) dropped entirely — replaced by event-bus interfaces stub per ADR-005.
  - `IConnectionFactory<TConnection>` introduced; `AConnection` is now Pipelines-based (abstract `OnPacketAsync`).
  - All packet classes rewritten: `Read(ref PacketReader r)` / `Write(ref PacketWriter w)` API, no connection or ByteBuffer in constructor.
- **Smoke run deferred.** M1 DoD item "SELECT 1 smoke" skipped — MySQL not reachable during this session. Must be verified before M2 DoD is closed. `SmokeHost` and `SchemaMigrationHost` are already wired in `Program.cs`.
- **Active milestone → M2.** See [`checklist.md`](checklist.md) and [`milestones/m2-execution-brief.md`](milestones/m2-execution-brief.md).

---

## 2026-04-28 · M5 code complete

- **Disconnect path implemented.** `GsClientConnection.DisposeAsync` now unregisters from `PlayerConnectionRegistry`, removes from `World`, saves position (`UpdatePositionAsync`), and sets `online=false` (`UpdateOnlineAsync`) using `CancellationToken.None` so saves complete even after the server's CT is cancelled.
- **IScriptHost concrete implementation.** `GameScriptHost` (`Game/Scripting/GameScriptHost.cs`) — simple primary-constructor record wrapping `IEventBus` + `ILogger<IScript>`.
- **Scripting wired into GameServerHost.** `LoadScriptsAsync` runs at startup: finds `Scripts/` folder beside the executable, calls `ScriptService.LoadAllAsync`, then wires a `FolderListenerService` for hot-reload. The `FolderListenerService` is disposed via `ct.Register`.
- **GsConnectionFactory + GsClientConnection extended.** Added `IPlayerDao`, `GameWorld`, `PlayerConnectionRegistry` to both — needed for the disconnect save path.
- **World / namespace clash.** Class `AionLightning.Game.World.World` conflicts with its own namespace wherever `using AionLightning.Game.World;` is present. Fixed everywhere with `using GameWorld = AionLightning.Game.World.World;` alias. Pattern established — apply to any new file referencing the World class.
- **M5 DoD status:** all code items complete; smoke tests (real client + DB) deferred. `Active milestone → M5 smoke` then M4/M6.

---

## 2026-04-28 · M5 world-entry layer coded

- **World entry flow coded** (0 errors / 0 warnings). Client can now enter the world and move.
- **Key decisions:**
  - `CM_ENTER_WORLD` (0xAA): reads objectId, validates account ownership, loads player + appearance from DB, sets HP/MP stubs (1000/500), registers in `PlayerConnectionRegistry`, sends `SM_CHARACTER_SELECT(0)` → `SM_STATS_INFO` → `SM_PLAYER_SPAWN` → `SM_GAME_TIME`.
  - `CM_LEVEL_READY` (0xAB): sends `SM_PLAYER_INFO` to self, then cross-introduces each online player (new→existing and existing→new via `SM_PLAYER_INFO`), then fires `PlayerEnteredWorldEvent` on the bus.
  - `CM_MOVE` (0xF2): reads x/y/z + heading + movementMask; conditionally reads vector or target position; updates `Player.Position` + movement fields; broadcasts `SM_MOVE` to all other online connections.
  - `SM_PLAYER_SPAWN` (0x0F): sends worldId (twice, second = channel), position, heading, and fixed flags. `isPersonal=0` (non-instanced world).
  - `SM_PLAYER_INFO` (0x20): templateId = `100000 + raceId * 2 + genderId` (same formula as Java `PlayerCommonData.getTemplateId()`). No items mask (0). All stats/skills/buffs stubbed.
  - `SM_STATS_INFO` (0x01): all combat stats are reasonable stubs (power/health/etc = 100, HP/MP stubs, attack range 5.0f). Sufficient for client to render the stat sheet without crashing.
  - `SM_CHARACTER_SELECT` (0xB1): `type=0` skips passkey prompt — the client transitions from character-select state to world-load state.
  - `SM_GAME_TIME` (0x26): minutes since 2000-01-01 UTC.
  - `MovementMask` constants extracted to `Model/MovementMask.cs` (StartMove=0x01, Mouse=0x02, Glide=0x04, Vehicle=0x08, Fall=0x10).
  - `PlayerConnectionRegistry` singleton: `ConcurrentDictionary<int, GsClientConnection>` keyed by objectId for broadcast operations.
  - `Player.Appearance` changed from `init` to `set` — allows assigning the loaded `PlayerAppearance` record after construction in `CM_ENTER_WORLD`.
  - `Player` gained movement fields: `MovementMask`, `VectorX/Y/Z`, `TargetX2/Y2/Z2`.
  - `World` class / `AionLightning.Game.World` namespace clash fixed via type alias `using GameWorld = AionLightning.Game.World.World` throughout all affected files.
  - `GsPacketHandlerFactory` gained `World`, `PlayerConnectionRegistry`, `IEventBus` constructor params; routes CM_ENTER_WORLD (AUTHED), CM_LEVEL_READY (IN_GAME), CM_MOVE (IN_GAME).
- **M5 is now code complete.** All scope items coded; smoke tests deferred (need MySQL + Aion 4.6.0 client).
- **Active milestone → M5 smoke** then M4 (Chat) or M6.

---

## 2026-04-28 · M5 character-select layer complete

- **Character select flow coded** (0 errors / 0 warnings). Client can now reach the character-select screen after GS auth.
- **Key decisions:**
  - `GsClientConnection` gains a `State` enum (`CONNECTED → AUTHED → IN_GAME`) and `AccountId`/`ActivePlayer` properties. `HandleLoginCheckAsync` transitions to `AUTHED` on success.
  - `GsPacketHandlerFactory` is state-aware: dispatches `CM_VERSION_CHECK`, `CM_CHARACTER_LIST`, `CM_MAY_LOGIN_INTO_GAME`, `CM_PING`, `CM_TIME_CHECK`, `CM_QUIT`, `CM_MAC_ADDRESS` by state.
  - `SM_VERSION_CHECK` sends simplified but valid response for client versions ≥ 204; rejects older clients with `0x02`.
  - `SM_CHARACTER_LIST` writes the full `PlayerInfo` wire format including appearance bytes, no item equipment data (empty `writeH(0)` for items) — sufficient to render characters in the client UI.
  - `WriteS(string, int)` fixed-width string overload added to `PacketWriter` (needed for 52-byte name field in character list).
  - `InMemoryEventBus` now supports runtime `Subscribe<T>`/`Unsubscribe<T>` for hot-reloaded scripts; DI handlers continue to work via `IServiceProvider.GetServices`.
  - `ScriptService` rewritten: collectible `AssemblyLoadContext(isCollectible:true)` per script file, `*.cs` glob scan, hot-reload via `FolderListenerService`, `WeakReference<AssemblyLoadContext>` for GC-safe unload.
  - `CSharpCompilerService` fixed: loads into collectible ALC, references all host assemblies via `AppDomain.CurrentDomain.GetAssemblies()`.
  - Game DB schema: `V1__initial_game.sql` with `players` + `player_appearance` tables (canonical 4.6.0 schema, minimal column set for M5).
  - `SchemaMigrationHost` added to Game project; `GameDb` connection string in `appsettings.json`.
  - `PlayerDaoImpl` + `PlayerAppearanceDaoImpl` use Dapper with column aliases matching the `record` property names.
- **Still pending in M5:** world entry (`CM_ENTER_WORLD`, `CM_LEVEL_READY`, `CM_MOVE`, `SM_PLAYER_SPAWN`, `SM_MOVE`), world grid/region system, `InventoryDao`, `IScriptHost` implementation wired into `GameServerHost`.
- **Active milestone → M5 world entry** (next session).

---

## 2026-04-28 · M3 code complete

- **All M3 scope items implemented** — 0 errors / 0 warnings with `TreatWarningsAsErrors`.
- **Key decisions:**
  - GS-LS protocol uses plain `[2-byte LE length][opcode(1)][body]` framing — NO Blowfish. The Java `GsServerPacket.write()` method confirmed the type byte inside `writeImpl()` IS the opcode byte; our `GsConnection.SendAsync` writes `packet.Opcode` directly, so `Write()` bodies must NOT repeat it.
  - `SM_GS_AUTH_RESPONSE` passes `serverCount` (not `serverId`) when `AUTHED` — matching the Java `SM_GS_AUTH_RESPONSE.writeImpl()`.
  - `accountsOnLs` pattern: `AccountController` maintains a `ConcurrentDictionary<int, Account>` of accounts that authenticated at LS but haven't transferred to GS yet. Prevents double-login, enables `CheckAuthAsync`.
  - `GameAccountRegistry` (GS-side): `ConcurrentDictionary<int, TaskCompletionSource<bool>>` bridges the async gap between `GsClientConnection.HandleLoginCheckAsync` (awaiting LS response) and `CM_ACCOUNT_AUTH_RESPONSE.RunAsync` (completing the TCS).
  - `LsConnectionHolder` singleton wraps the current `LsConnection?` on the GS side — allows `GsClientConnection` to reference a stable object while the underlying LS connection is replaced on reconnect.
  - `GsCrypt` XOR cipher: `serverKey[0..3]` = baseKey LE bytes, `[4..7]` = `{0xa1,0x6c,0x54,0x87}`. First `Encrypt()` call (for `SM_KEY`) is skipped, sets `isEnabled=true`. Opcode encoding: `(opcode + 0xCC) ^ 0xDD`.
  - `GameServerHost` runs two concurrent tasks: outbound LS reconnect loop + inbound Aion-client `TcpListener`. Reconnects every `ReconnectDelayMs` on LS loss.
  - `GsPingService` (LS-side): `PeriodicTimer` pings all AUTHED GS connections with `SM_PING`. `CM_GS_PONG` opcode = `0x0C`.
- **DoD status:** all scope items coded; DoD smoke tests deferred until MySQL + Aion 4.6.0 client are available.
- **Active milestone → M3 smoke** (DoD items), then M4/M5.

---

## 2026-04-28 · M2 code complete

- **All M2 scope items implemented** — 0 errors / 0 warnings with `TreatWarningsAsErrors`.
- **Key decisions:**
  - `SM_INIT` encrypted with **default** Blowfish key (same key the Aion 4.6.0 client hard-codes); `CryptEngine.UpdateKey(sessionKey)` called after send. All subsequent packets use the per-session key.
  - `LoginConnection.SendAsync`: layout = `[opcode(1)][body][zero-pad to 8-byte block boundary][checksum(4)]`; wire = `[2-byte LE length][encrypted block]`.
  - `KeyGen.DecryptRSA` added (BouncyCastle `RsaEngine`, no-padding) for `CM_LOGIN` RSA decryption of user/password.
  - `AccountController` converted to injectable singleton (`IAccountController`); password = `SHA-1(UTF-8(pw)) → Base64`.
  - `BannedIpController` refactored to depend on `IBannedIpDao`; `StartAsync` is async.
  - `GameServerTable.LoadFromConfig(GameServerEntry[], ILogger?)` replaces stub `Load()`; seeded from `appsettings.json "LoginServer:GameServers"` array in `LoginServerHost`.
  - `SM_UPDATE_SESSION` opcode confirmed **0x0C** (M2 brief had 0x02 erroneously).
  - `SM_SERVER_LIST` accepts `IEnumerable<GameServerInfo>` to side-step `ICollection` vs `IReadOnlyCollection` mismatch.
  - `LoginServer.cs` left compilable but unused (superseded by `LoginServerHost : BackgroundService`).
- **DoD status:** all scope items coded; DoD smoke tests (real client connect + DB account login) deferred until MySQL is available.
- **Active milestone → M2 smoke** (DoD items), then M3.

---

## 2026-04-23 · Cross-cutting closures (plan 10/10 prep)

- **Java reference worktree** created at `/tmp/aion-refs/4.6.0/` (and `/tmp/aion-refs/4.6.0/` as cross-reference). Resolves Q-1 / R-007. Setup command: `git worktree add /tmp/aion-refs/4.6.0 origin/4.6.0`.
- **Q-3 resolved: event bus.** Bespoke `Channel<T>`-based `InMemoryEventBus` replaces the MediatR option. MediatR v12+ is paid-license and adds per-publish reflection overhead unsuitable for tick-rate events. [ADR-005](adr/005-callbacks.md) updated.
- **Q-4 resolved: schema migration.** Evolve chosen (MIT, SQL-file versioning). Fresh [ADR-007](adr/007-schema-migration.md).
- **Password hash format confirmed 4.6.0.** `SHA-1(UTF-8(password))` → Base64 (no line separators, standard padding, no salt). Source: `/tmp/aion-refs/4.6.0/AL-Login/src/com/aionemu/loginserver/utils/AccountUtils.java`. Captured in [`data-schema.md`](data-schema.md).
- **Login packet opcodes confirmed 4.6.0.** Aion side: 5 client packets, 8 server packets, three states (`CONNECTED`, `AUTHED_GG`, `AUTHED_LOGIN`). GS side: 14 GS→LS + LS→GS opcodes. Source: `network/factories/{AionPacketHandlerFactory,GsPacketHandlerFactory}.java`. Moves into [M2](milestones/m2-execution-brief.md) and [M3](milestones/m3-execution-brief.md) execution briefs.
- **Scripts volume measured.** 2272 `.java` scripts + 605 XML files under `/tmp/aion-refs/4.6.0/AL-Game/data/`. Significant enough that [R-006](risks.md) is upgraded to High/High impact. Scope cut: starter zones only for first playable release.
- **Created:** [`agent-rules.md`](agent-rules.md) (15 rules for automated agents), [`packet-buffer-adapter.md`](packet-buffer-adapter.md) (full `PacketReader`/`PacketWriter` spec with concrete port examples for `CM_LOGIN` and `SM_INIT`).
- **Convention updates:** [`conventions.md`](conventions.md) now carries the `.editorconfig` block for packet naming and pins the Java reference location.

## 2026-04-23 · v2 plan started

- Verification found that `migration_plan.md` v1 suffers from checkbox drift — formal `[✓]` vs. real stubs (`DotNetAgentEnhancer = Console.WriteLine`, `ScriptService.Load = NotImplementedException`, `Dispatcher = while(true)+Thread.Sleep(1)+swallow Exception`). Details in [`current-state.md`](current-state.md).
- Strategy chosen: **code-driven rewrite**, not Strangler Fig. The end product is a single independent .NET server; the Java code stays in the repo as read-only reference, no runtime coexistence.
- Java baseline version: **4.6.0** (branch `4.6.0` / `4.6.0` — `4.6.0` used as primary), not 7.8. See [R-007](risks.md).
- Callbacks: **option A** selected — explicit event bus instead of AOP magic. See [ADR-005](adr/005-callbacks.md).
- Automated tests are not a gate on M1–M6; they live in their own milestone [M7](milestones/m7-verification-harness.md).
- Created the `migration/` structure: overall plan, checklist, ADRs, milestones, risks, glossary, journal, conventions, data schema, templates.
- Documentation language: English. User-facing chat replies remain in Ukrainian per personal preference.

---

<!-- new entries go above this line -->

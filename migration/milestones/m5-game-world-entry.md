# M5 — Game: world entry

Status: `[ ]` not started · Requires: M3 · ADRs: 001..006

## Goal

A player logs in through Login → GS, passes character select, enters the starting zone, sees their character, moves, and is visible to another player in the same zone. Logout writes state back to the DB.

This is the first milestone where the Game module does real work — and the first use of callbacks (ADR-005) and scripting (ADR-006).

## Scope

### In

- `AionLightning.Game` replaces the M3 skeleton with a real `BackgroundService` owning:
  - GS→LS link (reused from M3).
  - GS→Chat link (reused from M4).
  - Aion client listener (full packet pipeline).
- Core entities (4.6.2-compatible):
  - `Player` (identity, position, stats, appearance).
  - `Creature` (abstract base).
  - `VisibleObject` (abstract base with position / world-id).
  - `Position` (x, y, z, heading, world-id, instance-id).
- World grid / region system — simplified: a fixed-size cell grid per map; neighbour broadcast iterates adjacent cells. Full precise world model (as in the Java original) is deferred unless M5 smoke requires it.
- Character select + world entry packets (verify exact 4.6.2 set from `AL-Game/src/com/aionemu/gameserver/network/aion/clientpackets/` and `serverpackets/`).
- Movement packets: client move request → server validate (basic) → broadcast to neighbours.
- Logout packet flow with state persistence.
- DAOs (Dapper): `PlayerDao` (minimum: identity, position, appearance, last_online), `PlayerAppearanceDao`, `InventoryDao` (minimum — appearance-only items).
- **Event bus** (ADR-005) — sketched in M1, activated here:
  - Bus interface, in-process synchronous fan-out implementation.
  - First events: `PlayerEnteredWorldEvent`, `PlayerLeftWorldEvent`, `PlayerMovedEvent`.
  - Broadcast handler for world visibility.
- **Scripting service** (ADR-006) — rewritten here:
  - Collectible ALC per script folder.
  - `FolderListenerService` wired in for reload.
  - Host-side `IScriptHost` surface with `World`, `Events`, `Logger`.
  - No functional scripts loaded yet (M6.4 is quests); M5 only verifies the engine works by loading a "hello world" sample script.

### Out of scope

- AI / NPC spawns — M6.1.
- Combat / damage / skills — M6.1 / M6.2.
- Inventory beyond appearance — M6.3.
- Quests — M6.4.
- Instances, PvP, legions, trade — M6.5–M6.8.

## Dependencies

- Previous: M1, M2, M3 (M4 is orthogonal — M5 does not need Chat running).
- ADRs: 001, 002, 003, 004 (foundations); 005, 006 (first real use).
- Open questions to close before start: Q-2 (script translation approach), Q-3 (bus implementation MediatR vs. bespoke).

## Java source inventory

```bash
find AL-Game/src/com/aionemu/gameserver/world -name "*.java"
find AL-Game/src/com/aionemu/gameserver/model/gameobjects -name "Player.java" -o -name "Creature.java" -o -name "VisibleObject.java"
find AL-Game/src/com/aionemu/gameserver/dao -name "Player*.java" -o -name "Inventory*.java" -o -name "Appearance*.java"
find AL-Game/src/com/aionemu/gameserver/network/aion -name "*.java" | wc -l
find AL-Game/data -name "*.java" | wc -l   # scripts inventory
```

The `data/` scripts count decides how much of M6.4 needs extra time.

## .NET target layout

### New (Game)

- `AionLightning.Game/GameServerHost.cs` — extended from M3.
- `AionLightning.Game/World/World.cs`, `WorldMap.cs`, `Region.cs`.
- `AionLightning.Game/Model/GameObjects/VisibleObject.cs`, `Creature.cs`, `Player.cs`.
- `AionLightning.Game/Model/Position.cs`.
- `AionLightning.Game/Network/Aion/AionConnection.cs` — extended from M3.
- `AionLightning.Game/Network/Aion/ClientPackets/*.cs` — full 4.6.2 packet set for login → world entry → movement → logout.
- `AionLightning.Game/Network/Aion/ServerPackets/*.cs`.
- `AionLightning.Game/Dao/PlayerDao.cs`, `PlayerAppearanceDao.cs`, `InventoryDao.cs`.
- `AionLightning.Game/Controller/PlayerController.cs`, `WorldController.cs`.
- `AionLightning.Game/Events/*.cs` — event record types.

### New (Commons, consumed by Game)

- `AionLightning.Commons/Events/IEventBus.cs`, `InMemoryEventBus.cs`, `IEventHandler<T>.cs`.
- `AionLightning.Commons/Scripting/IScript.cs`, `IScriptHost.cs`, `ScriptService.cs` — full rewrite per ADR-006.
- `AionLightning.Commons/Scripting/CollectibleAlc.cs` — thin wrapper over `AssemblyLoadContext(isCollectible: true)`.

### Removed

- The partial `AionLightning.Commons/Services/ScriptService.cs` in its current shape (replaced by the ADR-006 rewrite).

## Definition of Done

- [ ] A player logs in through Login, picks character, enters the starting zone.
- [ ] Character appears at the correct spawn position, correct appearance.
- [ ] Movement (WASD) is smooth; server validates basic bounds (ignore obvious teleport-hacks) and broadcasts to neighbours.
- [ ] A second player logs in; sees the first player at the right position; movement updates propagate.
- [ ] Logout saves position + appearance to the DB. Re-login resumes at the same spot.
- [ ] A sample script loaded from `scripts/hello/` compiles and runs; modifying the file triggers reload; unload is clean (weak reference goes to null after GC).
- [ ] Event bus delivers `PlayerEnteredWorldEvent` to registered handlers in order; an exception in one handler does not stop the others; failures are logged.

## Smoke scenario

1. Start Login + GS.
2. Start two Aion clients, log in as two accounts with characters on the same faction and starting zone.
3. Both clients reach the world at the starting spawn.
4. Client A moves. Client B sees the movement.
5. Client A logs out; DB `players.pos_x/pos_y/pos_z` updated. Re-log in — same position.
6. Drop a `scripts/hello/Greeter.cs` that logs on `PlayerEnteredWorldEvent`. Re-enter. Expect a log line. Edit the script. Expect reload + updated log line.
7. Kill GS. Login marks it offline. Restart GS. Clients reconnect.

## Scoped risks

- **R-002 (Java sync patterns in tick loop):** world / region code in Java uses `synchronized` heavily. Port to async-friendly primitives carefully; if in doubt, prefer `lock` + synchronous tick callback over clever async.
- **R-005 (Cross-ALC type identity):** define the scripting contracts (`IScriptHost`, `IPlayer`) before compiling a single script. Scripts must never see `Player` directly.
- **R-006 (`data/scripts` volume):** inventory script count before starting M5 so the scope of M6.4 is known.
- **World-grid correctness:** it is easy to implement a grid that "mostly works" but fails at zone boundaries (players see each other from across a wall). Write a deterministic test in M7 for grid neighbour queries.
- **Position persistence rounding:** Aion uses float coordinates; DB columns are `FLOAT`. Verify no systematic drift on round-trip.

## Notes

- Keep the world grid small and pragmatic in M5. Aion's full world system (MapRegion, visibility filters) is significantly more involved — port what is strictly needed for "two players see each other and move".
- The event bus is performance-sensitive. M5 sets the shape; M6 will exercise it under load. Add a trace-level counter "events published per second" for later profiling.
- Scripting verification in M5 is deliberately boring — just a `Greeter` subscribed to `PlayerEnteredWorldEvent`. Reload the file and confirm the behaviour updates. Real script content arrives in M6.

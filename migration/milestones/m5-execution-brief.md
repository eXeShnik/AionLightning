# M5 — Execution brief

Scope in [`m5-game-world-entry.md`](m5-game-world-entry.md). **Read [`../agent-rules.md`](../agent-rules.md) first.**

## Preconditions

- M3 closed (M4 orthogonal — M5 does not require Chat).
- Q-2 resolved: scripts are rewritten into `.cs` (see [R-006](../risks.md)).
- Q-3 resolved: bespoke `Channel<T>`-based bus ([ADR-005](../adr/005-callbacks.md)).

## Reading scope (for agent — see [`../agent-prompt.md`](../agent-prompt.md) tiers)

**Tier A (mandatory):** `agent-rules.md`, `conventions.md`, `m5-game-world-entry.md`, this file.

**Tier B — must-read ADRs for M5** (largest Tier B of any milestone — M5 activates almost everything):
- [`../adr/001-networking.md`](../adr/001-networking.md) — GS Aion client listener.
- [`../adr/002-configuration.md`](../adr/002-configuration.md) — Game options, world config.
- [`../adr/003-hosting.md`](../adr/003-hosting.md) — `GameServerHost` extended from M3.
- [`../adr/004-database.md`](../adr/004-database.md) — Player / Appearance / Inventory DAOs.
- [`../adr/005-callbacks.md`](../adr/005-callbacks.md) — first real consumer of the event bus.
- [`../adr/006-scripting.md`](../adr/006-scripting.md) — full scripting rewrite.

**Tier B — step-triggered opens:**
- [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md) — when porting character-select / world-entry / movement packets.
- [`../data-schema.md`](../data-schema.md) — when implementing `PlayerDao` and friends.

**Do NOT read:** `adr/007-schema-migration.md` — schema is already wired in M1; no new migration tool work.

**Tier C — on-demand:**
- [`../risks.md`](../risks.md) — R-001 (ByteBuffer), R-002 (Java sync in tick loop — likely relevant for world grid), R-005 (cross-ALC identity — critical for scripting), R-006 (scripts volume — informs scope cut for first content).
- [`../glossary.md`](../glossary.md) — expect to grep this often; world / AI / scripting pull idioms (`ConcurrentHashMap.computeIfAbsent`, `ScheduledExecutorService`, `ClassLoader`) that translate non-trivially.

## Inventory (run at M5 start)

```bash
find /tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/network/aion/clientpackets -name "*.java" | wc -l
find /tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/network/aion/serverpackets -name "*.java" | wc -l
find /tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/world -name "*.java"
find /tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/model/gameobjects -name "Player.java" -o -name "Creature.java" -o -name "VisibleObject.java"
find /tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/dao -name "Player*.java" -o -name "Inventory*.java"
cat /tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/network/aion/AionPacketHandlerFactory.java
```

Out of that inventory, **pick only the packets needed for the world-entry flow** (listed below). The full game packet set is ported over M5+M6 — do not try to port all client packets in M5.

## Minimum packet set for world entry

Client → Server:

- `CM_ENTER_WORLD` — after character select, client sends a signed request to enter.
- `CM_CHARACTER_SELECTED` — after char-list, user picks a character.
- `CM_CHARACTER_LIST` — request character list.
- `CM_MOVE` — movement updates (coordinates + direction flags).
- `CM_QUIT` / `CM_LEAVE_WORLD` — logout.
- `CM_SHOW_MAP` / other minor packets the client requires after world entry — verify at inventory time.

Server → Client:

- `SM_CHARACTER_LIST` — list of characters on the account.
- `SM_CHARACTER_SELECTED` — selected character details.
- `SM_ENTER_WORLD` — confirms entry, sends initial location + appearance.
- `SM_NEAR_PLAYERS` / `SM_PLAYER_INFO` — broadcast of nearby players.
- `SM_MOVE` — movement relay to nearby clients.
- `SM_QUIT_RESPONSE` — ack logout.

Exact opcodes come from `AionPacketHandlerFactory.java` on the game server side. Capture into `milestones/m5-packet-inventory.md` at M5 start.

## Port order

1. Core entities (`AionLightning.Game/Model/GameObjects/`):
   - `VisibleObject` (position, world-id, object-id, optional owner).
   - `Creature : VisibleObject` (HP / MP fields).
   - `Player : Creature` (account link, name, appearance, PC class, faction, level, experience, gold, last-position).
   - `Position` struct (x / y / z / heading / world-id / instance-id).
2. DAOs (Dapper):
   - `PlayerDao`: `FindByAccountIdAsync`, `FindByIdAsync`, `CreateAsync`, `UpdatePositionAsync`, `UpdateAppearanceAsync`, `DeleteAsync`.
   - `PlayerAppearanceDao`: map `player_appearance` columns.
   - `InventoryDao` (minimum: equipped slots only; full impl in M6.3).
3. World / region:
   - `WorldMap` — static data from XML (stub: hard-code starting zones `210010000` Poeta and `220010000` Altgard for smoke; full XML reader in M5 phase-2).
   - `Region` — cell-based grid (`cellSize = 275f` Aion default — verify from Java `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/world/MapRegion.java`).
   - `World` — the root registry: all online players keyed by object-id; lookups by map, by region, by name.
4. Event bus implementation (`InMemoryEventBus`) per [ADR-005](../adr/005-callbacks.md). Interfaces already exist from M1.
5. Events: `PlayerEnteredWorldEvent`, `PlayerLeftWorldEvent`, `PlayerMovedEvent`, `PlayerAppearedEvent`, `PlayerDisappearedEvent`.
6. Scripting service rewrite ([ADR-006](../adr/006-scripting.md)):
   - `IScript`, `IScriptHost`, `CollectibleAlc`, rewritten `ScriptService`, `FolderListenerService` wired in.
   - Smoke script: `scripts/sample/Greeter.cs` implementing `IScript`, logs on `PlayerEnteredWorldEvent`.
7. `AionLightning.Game/Network/Aion/AionConnection` — full implementation (replaces M3 stub).
8. Packet classes in port order:
   - `CM_CHARACTER_LIST` → `SM_CHARACTER_LIST`.
   - `CM_CHARACTER_SELECTED` → `SM_CHARACTER_SELECTED`.
   - `CM_ENTER_WORLD` → `SM_ENTER_WORLD`.
   - `CM_MOVE` → `SM_MOVE`.
   - `CM_QUIT` → `SM_QUIT_RESPONSE`.
   - Broadcasting packets (`SM_PLAYER_INFO`, `SM_NEAR_PLAYERS`).
9. `GameServerHost` extended — owns client listener on `GameServer:Network:ClientPort`, orchestrates world shutdown on SIGTERM (save all players, flush DB).

## World grid specification

- Cell size: `275f` units (Aion 4.6.2 default). Verify in Java.
- Broadcast radius for "visible": 3×3 cells around the player's cell (9 total).
- Each `Region` holds `HashSet<VisibleObject>` guarded by a `lock` (sync is sufficient for M5 — async here complicates movement timing unnecessarily).
- Player move → recompute cell → if changed cell, remove from old `Region`, add to new, emit `PlayerDisappearedEvent` for cells lost, `PlayerAppearedEvent` for cells gained.

## Persistence cycle

- On `CM_ENTER_WORLD`: load player row + appearance + inventory slots → construct `Player` → add to `World` → broadcast.
- On `CM_MOVE`: in-memory update only. No DB write.
- On `CM_QUIT`: remove from `World` → save position + appearance to DB → close connection.
- Background flush: optional `BackgroundService` that every N minutes writes dirty positions (skip in M5 unless clients complain about lost state on hard shutdown — captured as M5 deferred optimisation).

## Scripting smoke

File `AionLightning.Game/Scripts/sample/Greeter.cs` (at runtime, not compile-time):

```csharp
using AionLightning.Commons.Scripting.Contracts;
using AionLightning.Commons.Events;

public sealed class Greeter : IScript, IEventHandler<PlayerEnteredWorldEvent>
{
    private IScriptHost _host = null!;
    public ValueTask InitializeAsync(IScriptHost host, CancellationToken ct)
    {
        _host = host;
        _host.Events.Subscribe<PlayerEnteredWorldEvent>(this);
        return ValueTask.CompletedTask;
    }
    public ValueTask HandleAsync(PlayerEnteredWorldEvent e, CancellationToken ct)
    {
        _host.Logger.LogInformation("Welcome, {Name}", e.Player.Name);
        return ValueTask.CompletedTask;
    }
}
```

## Smoke steps

1. LS + GS running; a real character row exists in `players` for the test account (pre-seeded via SQL or via `V2__seed_test_player.sql`).
2. Client A logs in, reaches character list (`SM_CHARACTER_LIST`).
3. Picks character, lands in Poeta starting zone.
4. Moves forward (WASD). No desync, smooth.
5. Log line from `Greeter`: "Welcome, <name>".
6. Client B logs in with a second account and character in the same zone.
7. Both clients see each other, movement broadcasts.
8. Client A logs out. `players.pos_x/y/z` updated. Client A re-logs → same position.
9. Edit `Greeter.cs` (change text). Expect reload log + updated greeting on next entry.
10. Drop `Greeter.cs` → no more greeting.

## Scoped risks

- [R-002](../risks.md) — sync patterns in tick loop. For M5 keep region-lock synchronous; revisit in M6.
- [R-005](../risks.md) — cross-ALC identity. `Greeter` is the first test; if it crashes with `InvalidCastException` on an event → fix the ALC boundary before any more scripts.
- [R-006](../risks.md) — 2272 java scripts total. M5 only loads one test script; translation of real scripts begins in M6.

## Exit criteria

- All DoD items in [`m5-game-world-entry.md`](m5-game-world-entry.md) ticked.
- Journal entry: `M5 closed: world entry live`.
- Active pointer → M6.

## References

- Java world root: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/world/`.
- Java gameobjects: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/model/gameobjects/`.
- Java character select packets: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/network/aion/{clientpackets,serverpackets}/`.
- Script sample target: `/tmp/aion-refs/4.6.2/AL-Game/data/scripts/system/handlers/`.

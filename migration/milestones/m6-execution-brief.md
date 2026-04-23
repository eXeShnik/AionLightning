# M6 — Execution brief

Scope in [`m6-game-content.md`](m6-game-content.md). **Read [`../agent-rules.md`](../agent-rules.md) first.**

M6 is a **coordination hub** — each sub-milestone gets its own execution brief when it starts. This file lays out shared conventions and the inventory discipline every sub-M follows.

## Reading scope (for agent — see [`../agent-prompt.md`](../agent-prompt.md) tiers)

Reading discipline is applied **per sub-milestone**, not once for M6 as a whole. The kick-off procedure below directs each sub-M's brief to declare its own "Reading scope" section following the same Tier A/B/C model.

**Tier A (mandatory when working in M6):** `agent-rules.md`, `conventions.md`, `m6-game-content.md`, this file, plus the active sub-milestone's scope + execution-brief pair.

**Tier B — typical M6 Tier B set** (sub-briefs may narrow):
- [`../adr/001-networking.md`](../adr/001-networking.md), [`../adr/002-configuration.md`](../adr/002-configuration.md), [`../adr/003-hosting.md`](../adr/003-hosting.md), [`../adr/004-database.md`](../adr/004-database.md) — foundational, reused.
- [`../adr/005-callbacks.md`](../adr/005-callbacks.md) — the event bus is used heavily across M6.
- [`../adr/006-scripting.md`](../adr/006-scripting.md) — especially for M6.1 (AI), M6.4 (quests), M6.5 (instances).
- [`../packet-buffer-adapter.md`](../packet-buffer-adapter.md) — when a sub-M ports packet classes.
- [`../data-schema.md`](../data-schema.md) — when a sub-M introduces new DAOs.

**Do NOT read:** `adr/007-schema-migration.md` — no migration-tool work.

**Tier C — on-demand:** `risks.md` (R-002, R-005, R-006 most relevant), `glossary.md` (frequently — world / AI / combat rely on Java sync + scheduling idioms).

## Universal sub-milestone procedure

For every sub-M (M6.1, M6.2, …):

1. **Kick-off**: create `milestones/m6-{n}-{name}-execution-brief.md` by copying [`../templates/milestone-template.md`](../templates/milestone-template.md) and filling in:
   - Scope (In / Out).
   - Dependencies (previous sub-M, ADRs).
   - Java inventory (list files, LOC, packets, DAOs).
   - Port order (topological).
   - DoD with a playable smoke scenario.
2. **Inventory**: run the shell snippet below scoped to the sub-M's Java subtree.
3. **Port**: follow the brief; commit per step; no `NotImplementedException` in reachable paths.
4. **Smoke**: demonstrate against the client; fail = block, regress = surface.
5. **Close**: tick the checklist + journal entry; move to next sub-M.

```bash
# Inventory template (adapt the find-path per sub-M)
SUBTREE=/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/ai
find "$SUBTREE" -name "*.java" | wc -l
find "$SUBTREE" -name "*.java" -exec wc -l {} + | tail -1
find /tmp/aion-refs/4.6.2/AL-Game/data/scripts/system/handlers/ai -name "*.java" | wc -l
```

## Shared patterns across sub-milestones

- Event bus is the backbone: every state change publishes an event; every handler subscribes explicitly.
- All game objects inherit `VisibleObject` from M5.
- Content templates come from XML under `/tmp/aion-refs/4.6.2/AL-Game/data/` — read via `System.Xml.Serialization` records, one reader per category (`ItemData`, `SkillData`, `NpcData`, `QuestData`, …).
- Scripts from `/tmp/aion-refs/4.6.2/AL-Game/data/scripts/` are **rewritten** as `.cs` ([R-006](../risks.md)), one sub-M at a time.

## Sub-milestone snapshots

### M6.1 — AI & combat base

- Java scripts directory: `/tmp/aion-refs/4.6.2/AL-Game/data/scripts/system/handlers/ai/`.
- Core engine: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/ai2/` (4.6.2 uses `ai2` package; verify).
- Events to publish: `CreatureAttackedEvent`, `CreatureDiedEvent`, `NpcAggroEvent`.
- Smoke: starter-zone monster spawns, aggros, chases, dies to auto-attack, respawns.

### M6.2 — Skills

- Java: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/skillengine/` + `data/skills/*.xml`.
- Port order: skill template reader → skill cooldown DAO → skill cast pipeline → effects (`DamageEffect`, `HealEffect`, `BuffEffect`) on event bus.
- Smoke: first-tier attack skill lands damage, cooldown enforced, nearby clients see the effect.

### M6.3 — Items & inventory

- Java: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/model/items/`, `dao/InventoryDAO.java`, `data/items/*.xml`.
- Events: `ItemEquippedEvent`, `ItemUnequippedEvent`, `ItemPickedUpEvent`.
- Smoke: pick up drop, equip, stats update, persist through logout.

### M6.4 — Quests

- Java scripts: `/tmp/aion-refs/4.6.2/AL-Game/data/scripts/system/handlers/quest/` (largest script directory).
- Java engine: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/questEngine/`.
- This is the most scripting-heavy sub-M. Translation strategy for quest scripts — decided at M6.4 start (either manual port of the 10–20 starter quests or a codegen converter). Scope cap per [R-006](../risks.md): Poeta + Altgard up to level 10 for the first playable release.
- Smoke: "Tursin's Axe" (starter quest) runs end-to-end.

### M6.5 — Instances

- Java: `/tmp/aion-refs/4.6.2/AL-Game/src/com/aionemu/gameserver/instance/`, scripts under `data/scripts/system/handlers/instance/`.
- Smoke: a low-level instance (e.g. Nochsana Training Camp) is entered, cleared, closed.

### M6.6 — Trade / broker / auction

- Java: `com.aionemu.gameserver.services.trade`, `broker`, `auction`.
- Smoke: player-to-player trade of an item; broker list + buy; auction bid + settle.

### M6.7 — Legions / groups

- Java: `com.aionemu.gameserver.model.team`, `model.team.legion`.
- Smoke: party kill + loot distribution; legion create + invite + storage.

### M6.8 — PvP / Abyss

- Java: `com.aionemu.gameserver.services.abyss`.
- Smoke: cross-faction PvP flags; Abyss points; one siege cycle (if siege assets fit the scope).

## Performance review after M6.2

After M6.2 lands, run a load test: 50 fake clients + 50 NPCs in one region, continuous movement + skill usage for 10 minutes. Record:

- Event bus throughput (publishes/sec).
- DB write rate.
- GC pressure (allocation/sec from `dotnet-counters`).

Numbers go into `journal.md`. Optimisations (if any) land as separate follow-up items, not blocking sub-milestones.

## Exit criteria (M6 umbrella)

- Every sub-milestone has its own execution brief with a recorded smoke result.
- At M6 close, a fresh player can: log in → pick starter quest → fight NPCs → gain XP → equip a drop → party up → run one low-level instance → PvP an opposing-faction character.
- No `NotImplementedException` on any path a level-1 to level-10 player can reach.

## References

- Java content root: `/tmp/aion-refs/4.6.2/AL-Game/`.
- Scripts root: `/tmp/aion-refs/4.6.2/AL-Game/data/scripts/`.
- XML content: `/tmp/aion-refs/4.6.2/AL-Game/data/static_data/`, `data/spawns/`, `data/skills/`, `data/quests/`.

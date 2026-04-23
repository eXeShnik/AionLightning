# M6 — Game content expansion

Status: `[ ]` not started · Requires: M5 · ADRs: 001..006

## Goal

Expand the .NET game server from "world entry + movement" to a playable subset of Aion 4.6.0. M6 is a sequence of sub-milestones, each a vertical slice ending in a client-observable scenario.

This file is the coordination hub. Each sub-milestone gets its own file when it starts (`m6-1-ai-combat.md`, `m6-2-skills.md`, ...). This document is updated with scope and DoD when sub-milestones are refined.

## Sub-milestones (draft order)

Order reflects dependency: M6.1 (AI/combat) is required by M6.2 (skills) and M6.5 (instances). M6.4 (quests) depends on M6.1 and M6.3 (items). PvP (M6.8) requires several earlier pieces.

### M6.1 — AI & combat base

Scope: NPC / monster spawns, idle-aggro-chase-return AI state machine, base combat formula (physical attack only), HP / MP model on events, death + respawn.

DoD: a starter-zone monster spawns, aggros on approach, chases, hits the player, dies to player auto-attack, respawns.

Blocking for: M6.2, M6.5.

### M6.2 — Skills

Scope: skill DAO, skill XML reader (from `AL-Game/data/`), skill-cast pipeline (client request → server validate → cooldown + cost → resolve effect → broadcast), effect system layered on the event bus from ADR-005.

DoD: player casts a first-tier attack skill; effect lands; cooldown enforced; damage visible; NPC reacts.

Blocking for: M6.8.

### M6.3 — Items & inventory

Scope: item DAO, item templates reader, inventory / equipment slots, equip / unequip with stat recalculation, cube (backpack) + warehouse.

DoD: player picks up a drop, equips a weapon, stats update correctly, item persists through logout.

Blocking for: M6.4, M6.6.

### M6.4 — Quests

Scope: quest engine (heavily scripting-driven — ADR-006), quest DAO, NPC-interaction dialog packets, quest progress state machine.

DoD: a starter quest from the 4.6.0 content runs end-to-end: accept, progress events update, complete, reward delivered.

Blocking for: nothing hard — further content comes in parallel with other sub-Ms.

### M6.5 — Instances / dungeons

Scope: instance manager (create, enter, leave, close after empty timeout), per-instance world state, teleport packets.

DoD: a low-level instance is entered by a party, cleared, rewarded, closed.

Blocking for: M6.8 (Abyss fortresses are instance-based).

### M6.6 — Trade / broker / auction

Scope: direct player-to-player trade, broker search + buy / sell, auction flow.

DoD: two players trade an item; same item is listed on the broker and purchased by a third.

### M6.7 — Legions / alliances / groups

Scope: group formation (invite / accept / leave), loot rules, legion creation / membership / ranks / storage / emblem.

DoD: party kills a monster, loot distributes per selected rule; legion create + member invite + legion-storage deposit works.

Blocking for: M6.8 (legion PvP).

### M6.8 — PvP / Abyss

Scope: faction mechanics (Elyos vs. Asmodian vs. Balaur — 4.6.0 set), PvP flagging, siege / Abyss points, Abyss rank updates.

DoD: two characters on opposing factions can PvP; winner earns Abyss points; one fortress siege cycle runs.

## Cross-cutting rules

- Each sub-M starts with a Java source inventory (list of packages touched + packet classes + DAO / controller classes).
- Each sub-M picks **one** vertical slice end-to-end; no "half AI + half skills" branching.
- Performance profiling may happen at the end of M6.2 (skills under load) and M6.7 (legion storage write patterns). Before that, correctness > performance.
- Content XML files under `AL-Game/data/` are read by a .NET reader in M6.1; do not copy-paste schemas into code.
- Scripts under `AL-Game/data/scripts/` are translated (or auto-generated) per Q-2. The decision must be made at M5 close.

## Dependencies

- Previous: M5.
- ADRs: all six.
- Open questions: Q-2 (scripts rewrite approach) must be resolved before M6.4.

## Definition of Done (M6 umbrella)

- [ ] All sub-milestones have their own file with scope + DoD.
- [ ] Each sub-milestone closes with a live smoke scenario demonstrated against a 4.6.0 client.
- [ ] No sub-milestone leaves `NotImplementedException` in a public API it claims to own.
- [ ] At the end of M6, a player can: log in, do a starter quest, equip gear, party up, run a low-level instance, PvP an opposing faction member.

## Notes

- Expect re-planning between sub-milestones. Aion content is deep; reality will force scope adjustments.
- If a sub-M stalls for mechanical reasons (XML schema surprises, opcode drift), prefer scoping that sub-M down rather than extending its timeline — keep the cadence.

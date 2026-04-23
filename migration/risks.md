# Risks and open questions

Each entry uses the same block format. Add new risks as their own section. Update mitigations and fallbacks over time.

---

## R-001 · `ByteBuffer` cursor semantics do not translate

- **Description.** Java `ByteBuffer.flip/compact/position/limit/mark` is a cursor-based model that has no direct .NET equivalent. `Span<T>`, `Memory<T>`, `ReadOnlySequence<byte>` use slice-based semantics instead.
- **Likelihood / impact.** High / High.
- **Trigger.** Porting the 100+ packet classes from `AL-*/network/**/clientpackets/*.java` and `serverpackets/*.java`.
- **Mitigation.** [ADR-001](adr/001-networking.md) pins `SequenceReader<byte>` for decoding and `ArrayBufferWriter<byte>` / `PipeWriter` for encoding. The concrete adapter API is specified in [`packet-buffer-adapter.md`](packet-buffer-adapter.md) with `ReadC`/`ReadH`/`ReadD`/`ReadQ`/`ReadS`/`ReadB` so packet classes port mechanically.
- **Fallback.** If the adapter misses an edge case (e.g. a negative-length skip caused by a Java bug), capture it in [`packet-buffer-adapter.md`](packet-buffer-adapter.md).

---

## R-002 · Java sync patterns in the game tick loop

- **Description.** The Aion gameserver relies heavily on `synchronized`, `ReentrantLock`, `ConcurrentHashMap.computeIfAbsent`, `AtomicInteger`, `CountDownLatch`. A 1:1 translation to `lock`/`ConcurrentDictionary`/`Interlocked` is tempting but several idioms are not equivalent.
- **Likelihood / impact.** Med / High.
- **Trigger.** M5+, when porting World / AI / combat.
- **Mitigation.** Document the canonical idiom mapping in [`glossary.md`](glossary.md). Critical cases:
  - `ConcurrentHashMap.computeIfAbsent` → `ConcurrentDictionary.GetOrAdd(k, _ => new Lazy<V>(...)).Value` (factory-overload `GetOrAdd` can invoke the factory multiple times).
  - `ScheduledExecutorService` → `PeriodicTimer` + `Channel<T>`, not `System.Threading.Timer` (different overlapping-callback semantics).
  - `volatile int` → `Volatile.Read/Write` or `Interlocked.*`; Java `volatile` is stronger than C# `volatile`.
- **Fallback.** Per-case review when a race condition shows up in smoke tests.

---

## R-003 · `static` initialisation order differs from Java

- **Description.** Java class init is lazy and deterministic. .NET `beforefieldinit` can run static ctors in surprising orders, particularly across assemblies.
- **Likelihood / impact.** Med / Med.
- **Trigger.** Anywhere a Java class holds a `static final` field that depends on another `static` initialiser. The worst offender is the `DataManager` pattern in the game module.
- **Mitigation.** No static init chains in new code. Everything goes through DI with an explicit `IHostedService.StartAsync` order. Stateless static util classes (`Rnd`, `Base64`) are fine.
- **Fallback.** If a ported Java class really needs static init — wrap it in `AsyncLazy<T>` and document the reason in code.

---

## R-004 · Checked exceptions disappear after the port

- **Description.** Java `throws IOException` forces the caller to handle it. C# enforces nothing. DAOs that relied on the compiler will silently swallow errors after a mechanical port.
- **Likelihood / impact.** High / Med.
- **Trigger.** Any DAO or network class ported from Java.
- **Mitigation.** Code-review every ported DAO / network class for the `try`/`catch` shape. Rule: `catch (Exception)` only at a process boundary; everywhere else — a specific type. Log results with context. [`agent-rules.md`](agent-rules.md) forbids generic swallow in agent-authored code.
- **Fallback.** At the first production incident — retrofit a `Result<T>` pattern in [ADR-004](adr/004-database.md) or spin up a dedicated ADR.

---

## R-005 · Cross-ALC type identity in scripting

- **Description.** Scripts are compiled into their own `AssemblyLoadContext(isCollectible:true)`. The `Player` type loaded inside a script ALC is not the same CLR type as `Player` in the host ALC. `is`/`as`/generic constraints break silently.
- **Likelihood / impact.** Med / High.
- **Trigger.** M5 (ADR-006) — as soon as scripts start to receive game entities.
- **Mitigation.** Scripts talk to the host only through **interfaces defined in Commons** (default ALC). A script never sees concrete `Player`/`NpcTemplate` types — only `IPlayer`/`INpcTemplate`. Enforced by [`agent-rules.md`](agent-rules.md) rule 7.
- **Fallback.** If performance suffers from extra boxing / interface dispatch — revisit in M6; we may drop collectible ALCs and live with a memory leak on (rare) reloads.

---

## R-006 · Java 4.6.2 scripts in `data/scripts/**/*.java`

- **Description.** The Aion server ships **2272 `.java` scripts** (quests, NPC behaviour, spawn logic) and **605 XML data files**. Confirmed at 4.6.2 inventory (`find /tmp/aion-refs/4.6.2/AL-Game/data -name "*.java" | wc -l`).
- **Likelihood / impact.** High / High (volume is definitively large).
- **Trigger.** M5 (scripting service), M6.4 (quests).
- **Mitigation.** Scripts are organised by category (`scripts/system/handlers/{admincommands,ai,quest,instance,zone,playercommands,languages,weddingcommands}`, `scripts/custom/`). Port order matches M6 sub-milestones:
  - M5: `system/database/` (DAO helper scripts) only, plus one `handlers/` smoke script.
  - M6.1: `handlers/ai/`.
  - M6.4: `handlers/quest/`.
  - M6.5: `handlers/instance/`.
  - M6.7/6.8: `handlers/zone/`, `handlers/playercommands/`.
  - M6 or deferred: `handlers/admincommands/`, `handlers/weddingcommands/`, `handlers/languages/`, `custom/`.
- **Fallback.** Scope the first playable release to starter zones (Poeta, Altgard — level 1–10). Roughly 5–10% of the total script surface.

---

## R-007 · 4.6.2 vs 7.8 client packet drift — RESOLVED

- **Status:** Resolved on 2026-04-23.
- **Description.** The current branch `dot_net_10_migration` was forked off `7.8.0`. The migration baseline is 4.6.2. Reading Java from the current working copy risks picking up the 7.8 variant.
- **Resolution.** Git worktrees created once for both reference branches:
  - `/tmp/aion-refs/4.6.2` (primary baseline).
  - `/tmp/aion-refs/4.6.0` (cross-reference).
- **Usage rule.** All Java references in migration docs and agent workflows point at `/tmp/aion-refs/4.6.2/AL-*/...`. Agents **must not** read Java sources from the current working tree when porting classes. See [`conventions.md`](conventions.md) → Java reference location.
- **Verification.** The 4.6.2 worktree is recreated on a fresh machine via `git worktree add /tmp/aion-refs/4.6.2 origin/4.6.2`.

---

## R-008 · Java server has not run for a while

- **Description.** If the Java server has not been booted in years, we cannot fall back to golden traces (packet captures) as a reference.
- **Likelihood / impact.** Unknown / Med.
- **Trigger.** When a .NET packet implementation disagrees with client expectations and we need a reference.
- **Mitigation.** Primary reference is the Java **source** from `/tmp/aion-refs/4.6.2/`; every packet class and handler factory is readable offline. In M7 — optional capture of baseline traces from a live Java server if stand-up is cheap.
- **Fallback.** Reverse-engineer from Java code without running it (the full source is available offline).

---

## R-009 · `data/` XML content files

- **Description.** `AL-Game/data/` contains 605 XML files describing static content (NPC templates, items, skills, locations, quests).
- **Likelihood / impact.** Med / Med.
- **Trigger.** M5+, when the game engine starts reading content.
- **Mitigation.** The XML schema is stable; JAXB-generated Java classes map 1:1 to `System.Xml.Serialization` records. The XML files themselves are copied verbatim into the .NET output.
- **Fallback.** If JAXB classes are too entangled — switch to `System.Text.Json` against pre-converted JSON. Decision in M5.

---

## R-010 · Aion client legal context

- **Description.** Aion is a commercial NCSOFT client. Smoke testing needs the client. Access confirmed by the repo owner.
- **Likelihood / impact.** N/A during development / High when publishing a repo that contains client artefacts.
- **Trigger.** If we publish the repo / CI with client files.
- **Mitigation.** Client stays out of the repo. `.gitignore` must cover client folders. `README.md` states "bring your own client" — no redistribution.
- **Fallback.** If NCSOFT / licensors send a cease and desist — privatise / archive the repo.

---

## Open questions — all resolved

| # | Question | Resolution |
|---|---|---|
| Q-1 | How do we access the Java 4.6.2 reference? | **Resolved.** Git worktree at `/tmp/aion-refs/4.6.2/` (see R-007). Setup command: `git worktree add /tmp/aion-refs/4.6.2 origin/4.6.2`. |
| Q-2 | Do we rewrite `AL-Game/data/scripts/**/*.java` into `.cs`, or keep a Java-script-like execution mechanism? | **Resolved.** Rewrite into `.cs`. Scope limited per R-006. Port order follows M6 sub-milestones. |
| Q-3 | MediatR or a bespoke `Channel<T>`-based bus? | **Resolved.** Bespoke `Channel<T>`-based bus. MediatR is a paid license (v12+) with runtime reflection per publish — unacceptable for a tick-rate event hot path. See [ADR-005](adr/005-callbacks.md). |
| Q-4 | Schema-migration tool (FluentMigrator / Evolve / raw SQL files)? | **Resolved.** Evolve. SQL files from `AL-Login/sql/` and `AL-Game/sql/` copied verbatim, versioned by Evolve. See [ADR-007](adr/007-schema-migration.md). |
| Q-5 | Do we need CI from M1 onwards, or is it deferred to M7? | **Resolved.** Deferred to M7. |

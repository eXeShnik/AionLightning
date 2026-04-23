# Journal

Chronological log of key decisions about the migration. Newest entries on top. Format: date → short decision → links to ADR / milestone / risk.

---

## 2026-04-23 · Cross-cutting closures (plan 10/10 prep)

- **Java reference worktree** created at `/tmp/aion-refs/4.6.2/` (and `/tmp/aion-refs/4.6.0/` as cross-reference). Resolves Q-1 / R-007. Setup command: `git worktree add /tmp/aion-refs/4.6.2 origin/4.6.2`.
- **Q-3 resolved: event bus.** Bespoke `Channel<T>`-based `InMemoryEventBus` replaces the MediatR option. MediatR v12+ is paid-license and adds per-publish reflection overhead unsuitable for tick-rate events. [ADR-005](adr/005-callbacks.md) updated.
- **Q-4 resolved: schema migration.** Evolve chosen (MIT, SQL-file versioning). Fresh [ADR-007](adr/007-schema-migration.md).
- **Password hash format confirmed 4.6.2.** `SHA-1(UTF-8(password))` → Base64 (no line separators, standard padding, no salt). Source: `/tmp/aion-refs/4.6.2/AL-Login/src/com/aionemu/loginserver/utils/AccountUtils.java`. Captured in [`data-schema.md`](data-schema.md).
- **Login packet opcodes confirmed 4.6.2.** Aion side: 5 client packets, 8 server packets, three states (`CONNECTED`, `AUTHED_GG`, `AUTHED_LOGIN`). GS side: 14 GS→LS + LS→GS opcodes. Source: `network/factories/{AionPacketHandlerFactory,GsPacketHandlerFactory}.java`. Moves into [M2](milestones/m2-execution-brief.md) and [M3](milestones/m3-execution-brief.md) execution briefs.
- **Scripts volume measured.** 2272 `.java` scripts + 605 XML files under `/tmp/aion-refs/4.6.2/AL-Game/data/`. Significant enough that [R-006](risks.md) is upgraded to High/High impact. Scope cut: starter zones only for first playable release.
- **Created:** [`agent-rules.md`](agent-rules.md) (15 rules for automated agents), [`packet-buffer-adapter.md`](packet-buffer-adapter.md) (full `PacketReader`/`PacketWriter` spec with concrete port examples for `CM_LOGIN` and `SM_INIT`).
- **Convention updates:** [`conventions.md`](conventions.md) now carries the `.editorconfig` block for packet naming and pins the Java reference location.

## 2026-04-23 · v2 plan started

- Verification found that `migration_plan.md` v1 suffers from checkbox drift — formal `[✓]` vs. real stubs (`DotNetAgentEnhancer = Console.WriteLine`, `ScriptService.Load = NotImplementedException`, `Dispatcher = while(true)+Thread.Sleep(1)+swallow Exception`). Details in [`current-state.md`](current-state.md).
- Strategy chosen: **code-driven rewrite**, not Strangler Fig. The end product is a single independent .NET server; the Java code stays in the repo as read-only reference, no runtime coexistence.
- Java baseline version: **4.6.2** (branch `4.6.0` / `4.6.2` — `4.6.2` used as primary), not 7.8. See [R-007](risks.md).
- Callbacks: **option A** selected — explicit event bus instead of AOP magic. See [ADR-005](adr/005-callbacks.md).
- Automated tests are not a gate on M1–M6; they live in their own milestone [M7](milestones/m7-verification-harness.md).
- Created the `migration/` structure: overall plan, checklist, ADRs, milestones, risks, glossary, journal, conventions, data schema, templates.
- Documentation language: English. User-facing chat replies remain in Ukrainian per personal preference.

---

<!-- new entries go above this line -->

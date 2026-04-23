# Migration (Java 4.6.2 → .NET 10)

Working documentation for migrating AionLightning from Java 1.7 to .NET 10. This directory is live — content is updated as work progresses.

## Goal

Rewrite the four servers (Commons shared, Login, Chat, Game) into an independent .NET product. The Java codebase remains in the repo as a read-only reference; no runtime coexistence between the two stacks is planned.

## Where to start reading

1. [`plan.md`](plan.md) — overall plan: strategy, milestones, architectural decisions.
2. [`current-state.md`](current-state.md) — honest reassessment of what is already in `AionLightning.NET/`.
3. [`checklist.md`](checklist.md) — status and DoD checkboxes per milestone.
4. [`milestones/`](milestones/) — detailed execution plans, one file per milestone.
5. [`adr/`](adr/) — architectural decisions accepted for this project.
6. [`agent-prompt.md`](agent-prompt.md) — copy-paste prompt template for running a milestone with an automated agent (substitute `{N}` with the milestone number).

Supporting docs:

- [`agent-rules.md`](agent-rules.md) — 15 execution rules automated agents must follow.
- [`packet-buffer-adapter.md`](packet-buffer-adapter.md) — full `PacketReader` / `PacketWriter` spec.
- [`risks.md`](risks.md) — risks and open questions.
- [`glossary.md`](glossary.md) — Java concepts and their .NET equivalents, with typical pitfalls.
- [`conventions.md`](conventions.md) — project-specific conventions (naming, layout).
- [`data-schema.md`](data-schema.md) — DB schema, SQL scripts, migrations.
- [`journal.md`](journal.md) — chronological log of decisions.
- [`templates/`](templates/) — blank templates for new milestone/ADR/risk entries.

## Archived plan

Previous plan: [`../migration_plan.md`](../migration_plan.md). Kept as historical reference. Its statuses are out of date — the authoritative view lives in [`current-state.md`](current-state.md) and [`checklist.md`](checklist.md).

## Java source baseline

**4.6.2** (branch `4.6.0` of the `ZON3DEV/AionLightning` repo). Packet layouts, DB schema, DAO SQL and game constants all come from there. The current working branch `dot_net_10_migration` was forked off `7.8.0`, so when porting individual classes verify you are reading the 4.6.2 variant (see [R-007](risks.md)).

## Target platform

.NET 10 (`net10.0`). `AionLightning.NET/Directory.Build.props` pins the framework version and `Microsoft.Extensions.*` 10.0.2.

## Document language

All files in `migration/` are written in English. Commit messages, code comments, and ADR bodies follow the same rule.

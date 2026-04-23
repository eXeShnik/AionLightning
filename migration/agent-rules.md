# Agent execution rules

Rules for automated agents (Sonnet, Haiku, Opus) working on this migration. These rules are read first before any other file. An agent that does not load or follow these rules must not be trusted to make progress.

## Rule 1 — Java source of truth

All Java references come from the dedicated worktree (call it `$JAVA_REF`), **not** from `AL-Login/`, `AL-Commons/`, `AL-Game/`, `AL-Chat/` in the current working tree. The current working tree is 7.8, not 4.6.2.

- The default `$JAVA_REF` in all migration docs is `/tmp/aion-refs/4.6.2/`. If you set the worktree up elsewhere (e.g. Windows at `C:/temp/aion-refs/4.6.2/`, or a durable `~/work/aion-refs/4.6.2/`), substitute that path in every reference below.
- When invoking an agent via [`agent-prompt.md`](agent-prompt.md), pass your actual path as the `{JAVA_REF}` placeholder.
- Before starting any port, verify the worktree exists: `ls $JAVA_REF/AL-Login/src/com/aionemu/loginserver/controller/AccountController.java`.
- If missing, recreate: `git worktree add $JAVA_REF origin/4.6.2` (run from the repo root).
- Every time a Java file is read for porting, the path is anchored at `$JAVA_REF/...` — never at `AL-*/` relative to the repo root.

## Rule 2 — Read the milestone's execution brief first

Every milestone has `milestones/m{N}-*.md` (scope + DoD) **and** `milestones/m{N}-execution-brief.md` (step-by-step work plan). Read both before touching code. The brief is authoritative for file order, code patterns, and verification steps.

If the brief is missing or its preconditions are not met, **stop** and surface the gap — do not improvise.

## Rule 3 — Close scope; do not improvise

If a step in the execution brief refers to Java logic that is not in the current milestone's scope (e.g. a packet handler calls into a subsystem not yet ported), **stop**. Do not invent a stub. Surface the gap with:

- Java file path: `/tmp/aion-refs/4.6.2/.../X.java`
- Line numbers of the problem area.
- What the Java code expects that the current .NET scope does not provide.
- A proposed scope expansion or a temporary boundary.

## Rule 4 — `NotImplementedException` is a blocker

In any file that is in the current milestone's scope and on a reachable code path during that milestone's smoke scenario, `throw new NotImplementedException()` is forbidden.

If a method cannot be implemented within scope, Rule 3 applies: stop and surface.

## Rule 5 — async + CancellationToken, no `new Thread().Start()`

All new I/O-bound methods are `async Task` / `async ValueTask` with `CancellationToken ct` as the last parameter. `CancellationToken` is propagated, never defaulted. No `.Result`, no `.Wait()`, no `GetAwaiter().GetResult()`.

`new Thread().Start()` is banned in new code. Long-running loops run as `BackgroundService.ExecuteAsync` or `Task.Run(async ...)` with explicit cancellation.

## Rule 6 — No generic catch without re-raise or explicit reason

`catch (Exception)` is allowed only at a process boundary (e.g. a packet handler that must not crash the server). Everywhere else, catch the specific exception type. Every `catch` logs the caught exception with context.

Forbidden:

```csharp
catch (Exception) { }
catch (Exception e) { log.LogError(e.Message); } // lost stack trace
```

Required:

```csharp
catch (MySqlException ex) { _log.LogError(ex, "DB call failed for {Account}", accountId); throw; }
```

Reasoning: see [R-004](risks.md).

## Rule 7 — Scripts never see concrete host types

Scripting code (loaded through `ScriptService`) interacts with the host only through interfaces declared in `AionLightning.Commons.Scripting.Contracts.*` — never `Player`, `Creature`, `World` directly. Rationale: cross-ALC type identity ([R-005](risks.md)).

## Rule 8 — Config goes through `IOptions<T>` + record DTO

No new static config classes. No `public static string X { get; set; }` on any config-like class. All configuration flows through records registered via `AddOptions<T>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()` ([ADR-002](adr/002-configuration.md)).

## Rule 9 — DB access goes through `MySqlDataSource` + Dapper

DAO classes receive `MySqlDataSource` via constructor injection. Every method opens a connection with `await using var conn = await ds.OpenConnectionAsync(ct)`. Parameters are named (`@x`), never string-interpolated. See [ADR-004](adr/004-database.md).

## Rule 10 — Packet classes keep Java names

Files under `Network/*/{ClientPackets,ServerPackets}/` keep the Java class names verbatim: `CM_LOGIN.cs`, `SM_INIT.cs`. Do not "PascalCase" them; do not rename to `ClientLogin`. The rationale is grep symmetry with the 4.6.2 Java source.

Analyzer warnings for naming style are suppressed via the project-level `.editorconfig` (delivered in M1).

## Rule 11 — Work in visible commits

After completing a sub-scope (e.g. a DAO + its tests, a single packet implementation + handler registration), create a commit with a message `M{N}: <concrete action>`. Do **not** batch multiple unrelated changes. Do **not** commit broken state.

If work must pause mid-step, stop on a compiling tree and leave `// WIP (agent): <what is missing>` at the top of the unfinished file, with a description of the remaining work.

## Rule 12 — Report before finishing

Before marking a milestone / task complete, summarise:

1. Files created, rewritten, deleted.
2. Ported Java sources (paths under `/tmp/aion-refs/4.6.2/`).
3. Smoke scenario result (passed / failed / blocked).
4. Any deviations from the execution brief.
5. Any new risks to add to [`risks.md`](risks.md).

## Rule 13 — When in doubt, stop

The cost of a wrong implementation decision in protocol-level code is several hours of downstream debugging. The cost of asking a clarifying question is seconds. Preference: **stop and ask**.

This is especially true for:

- Packet field types and offsets.
- Cryptographic order (which packet is encrypted with which key).
- Database column semantics (`access_level` levels, `membership` flags).
- Floating-point vs. integer coordinates.
- Endianness of a specific field.

The full Java source is at `/tmp/aion-refs/4.6.2/` — reading it is almost always cheaper than guessing.

## Rule 14 — Never commit secrets

Any DB password, shared secret, or key material stays in `appsettings.Development.json` (gitignored) or environment variables. Never in a tracked `appsettings.json`.

## Rule 15 — Keep the journal

Meaningful decisions (an ADR amendment, a risk discovery, a scope cut) land in [`journal.md`](journal.md) with a dated bullet and a cross-link.

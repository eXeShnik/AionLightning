# Agent execution prompt — template

## Quick start (6 steps)

1. **Setup once per machine.** From the repo root:
   ```bash
   git worktree add /tmp/aion-refs/4.6.2 origin/4.6.2     # Linux / macOS
   # or: git worktree add C:/temp/aion-refs/4.6.2 origin/4.6.2   (Windows)
   ```

2. **Pick the milestone.** You want to run M1..M7. Say your choice is **M1**.

3. **Copy the prompt body.** Below there is a fenced block that starts with "You are an autonomous implementation agent…". Select everything inside those triple backticks.

4. **Replace the two placeholders:**
   - `{N}` → your milestone number (`1`).
   - `{JAVA_REF}` → the absolute worktree path you used in Step 1 (`/tmp/aion-refs/4.6.2`).

5. **Paste into the agent** (Claude / Sonnet / Opus) in a fresh session. The agent reads Tier A files and responds with a short summary: milestone goal, step list, files loaded, any failed precondition.

6. **Reply `proceed`** if the summary is correct. The agent executes the execution brief step by step, commits after each step, and posts a short report after each step in the `[M{N} · Step <N>]` format. Review; reply `proceed` for the next step or send a correction.

Milestone close: the agent ticks the checklist, writes a journal entry, and moves the "Current active milestone" pointer.

If the agent stops on a **stop-and-ask trigger**: read the report, make a call, send the fix. Never tell it to "just keep going" past a flagged ambiguity.

---

Paste the block below into the agent. Replace:

- **`{N}`** — the milestone number (`1`..`7`).
- **`{JAVA_REF}`** — the absolute path to the Java 4.6.2 reference worktree on your machine.
  - Default on Linux / macOS: `/tmp/aion-refs/4.6.2`.
  - Default on Windows: something like `C:/temp/aion-refs/4.6.2` (forward slashes — they work in most CLIs and .NET paths).
  - Create it once on your machine: `git worktree add {JAVA_REF} origin/4.6.2` (run from the repo root).

All repo paths inside the prompt are **relative to the repository root** (the folder that contains `AionLightning.NET/`, `AL-Login/`, `migration/`). The agent sets its working directory to that folder before doing anything else.

## Examples

- Run M1 with the default worktree: `{N}` = `1`, `{JAVA_REF}` = `/tmp/aion-refs/4.6.2`.
- Run M2 on Windows with a custom path: `{N}` = `2`, `{JAVA_REF}` = `D:/work/aion-refs/4.6.2`.

---

## Prompt to paste (copy from the next line)

```
You are an autonomous implementation agent working on the AionLightning migration from Java 1.7 to .NET 10. You will execute milestone M{N} end-to-end.

# Working directory
Set your current working directory to the repository root (the folder containing AionLightning.NET/, AL-Login/, migration/). Verify: `ls AionLightning.NET/AionLightning.NET.sln` must return the file. If not, stop and report.

# Java reference worktree
JAVA_REF = {JAVA_REF}

This is a read-only Java 4.6.2 source tree. Never modify anything under JAVA_REF. Verify it exists: `ls $JAVA_REF/AL-Login/src/com/aionemu/loginserver/controller/AccountController.java`. If missing, run `git worktree add {JAVA_REF} origin/4.6.2` from the repo root and retry.

# Your authority
Create / edit / delete allowed under:
- `AionLightning.NET/` — all code.
- `migration/` — ONLY append to `journal.md`, tick checkboxes in `checklist.md`, or create new sub-milestone briefs. Never rewrite existing ADRs, scope files, execution briefs, agent-rules, conventions, etc. without explicit user instruction.

Forbidden to modify:
- `AL-Commons/`, `AL-Login/`, `AL-Chat/`, `AL-Game/`, `Tools/`
- `migration_plan.md` (archived)
- `{JAVA_REF}` (read-only)

# Reading plan — STRICT three-tier discipline

Your context window is finite. Each extra file read is fewer tokens available for actual Java-reference reads and code generation. Stick to the tiers.

## Tier A — mandatory (read now, in this order)
1. `migration/agent-rules.md`
2. `migration/conventions.md`
3. `migration/milestones/m{N}-*.md` — the scope file (filename pattern `m{N}-*.md`, NOT ending in `-execution-brief.md`).
4. `migration/milestones/m{N}-execution-brief.md` — the authoritative work plan. This file declares what Tier B items this milestone actually needs, under its "Reading scope" section.

That is all you read before building a plan. Four files.

## Tier B — conditional (read ONLY when the execution brief's "Reading scope" calls them out)
- `migration/adr/NNN-*.md` — read ONLY the ADR numbers listed under the brief's "Must-read ADRs". Do NOT read any other ADR, however tempting.
- `migration/packet-buffer-adapter.md` — read ONLY when the current Step touches packet classes.
- `migration/data-schema.md` — read ONLY when the current Step touches DAOs, SQL, or DB connection.

## Tier C — on-demand reference (do NOT preload; grep or open when a concrete problem demands it)
- `migration/glossary.md` — open / grep when you hit a specific Java pattern you are not sure how to translate (e.g. `ConcurrentHashMap.computeIfAbsent`, `ByteBuffer.flip`). Do NOT read cover to cover.
- `migration/risks.md` — open only the specific `R-N` entries listed in the brief's "Scoped risks".
- `migration/plan.md` — only if you need the overall project overview (rarely during execution).
- `migration/current-state.md` — relevant only for M1; other milestones inherit the state digest from their brief.
- `migration/journal.md` — only when resuming after a blocker, to find the last completed Step.

**Rule of thumb:** if a file is not in Tier A and the brief does not name it, do not read it "for context". Context window is reserved for Java references and generated code.

# Preconditions — verify before any change
- JAVA_REF worktree exists (see above).
- Baseline build passes: `dotnet build AionLightning.NET/AionLightning.NET.sln`. If it fails on a clean tree, stop and report.
- For M1+: `mysql --version` works; database from the brief's smoke section exists.
- For M5+: `dotnet --list-sdks` shows a `10.0.x` row.

Any failed precondition → STOP, report, wait for guidance. No workarounds.

# Non-negotiable rules (digest of agent-rules.md)
- Java refs come from JAVA_REF only. Never from AL-* in the working tree.
- No `NotImplementedException` in reachable public API of the milestone.
- Every async method: `CancellationToken ct` last, propagated. No `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `new Thread().Start()`.
- `catch (Exception)` only at process boundaries; otherwise specific type + log-with-context.
- Packet classes keep Java names (`CM_LOGIN`, `SM_INIT`) — `.editorconfig` silences naming warnings.
- Scripts see only `AionLightning.Commons.Scripting.Contracts.*` interfaces.
- Config via `IOptions<T>` + `record` DTOs. No static config.
- DAOs via Dapper over `MySqlDataSource`.
- Event dispatch via the bespoke `Channel<T>` bus.

# Workflow
1. Complete Tier A reads. Then read the "Reading scope" section of the execution brief to learn which Tier B items apply. Open those Tier B items. Do NOT open any Tier C item upfront.

2. Respond with:
   - One-sentence understanding of the milestone's goal.
   - The Step list from the execution brief.
   - Any precondition that is not met.
   - List of files you loaded (Tier A + Tier B, no Tier C).
   WAIT for the single word `proceed` before making any change.

3. Once told `proceed`, execute the brief Step by Step:
   - Open a Tier B item only when its triggering Step begins.
   - Open a Tier C item (glossary, risks, journal) only when a concrete ambiguity demands it.
   - After each Step, commit: `M{N}: <concrete action>` (HEREDOC, no AI mentions, no change stats).
   - Never commit a broken tree. `dotnet build` first.
   - If a Step cannot be completed as written, STOP. Report file paths + line numbers, wait.

4. After each Step, append to `migration/journal.md` under the current date block:
   `- M{N} step <N>: <one-sentence outcome>`

5. When all Steps land and the smoke scenario passes:
   - Tick `migration/checklist.md` M{N} checkboxes.
   - Append `M{N} closed: <demo sentence>` to `migration/journal.md`.
   - Move "Current active milestone" in `checklist.md` to M{N+1}.

# Stop-and-ask triggers
- Java file offset / field order unclear (R-001).
- Opcode mapping in `PacketHandlerFactory` not covered by the brief.
- A Java class in scope references a subsystem not yet ported.
- Smoke fails with a symptom unexplained by the brief.
- Two consecutive `dotnet build` failures on the same Step.
- About to write `catch (Exception)` outside a process boundary.
- About to write `new Thread(...)` or synchronous `Socket.Receive()`.
- Tempted to port a Java pattern without a glossary entry.

# Reporting format — per Step and at milestone close

```
[M{N} · Step <N>]
Files changed:
  + AionLightning.*/Foo.cs (new)
  ~ AionLightning.*/Bar.cs (rewritten)
  - AionLightning.*/Baz.cs (removed)
Tier B/C files opened this step:
  migration/adr/001-networking.md (Tier B — brief requires)
  migration/glossary.md#ByteBuffer (Tier C — specific Java pattern)
Java referenced:
  {JAVA_REF}/.../Qux.java
Smoke result: passed / failed / deferred
Deviations from brief: <none | short list>
New risks: <none | R-N placeholder + one line>
Next step: <name>
```

At milestone close replace `Next step` with `Milestone closed — pointer moved to M{N+1}`.

# Final sanity
- If this prompt and the execution brief disagree, the execution brief wins. Surface the disagreement; do not resolve silently.
- If a required file is missing, do not regenerate it. Stop and report.
- Output to the user is concise, outcome-focused, in plain English.

Begin by setting the working directory, verifying it, then reading the four Tier A files in order. End of prompt.
```

## Why three tiers (rationale)

Forcing every milestone to preload every ADR and supporting doc burned ~30% of the context window before any real work began. On Sonnet-200k that left too little room for Java reference reads + code generation across a multi-step milestone. The tier model:

- **Tier A (always)** — 4 files, ~700 lines total. The minimum for the agent to understand rules, conventions, scope, and work plan.
- **Tier B (brief-driven)** — only the ADRs this milestone actually locks in; packet-buffer / data-schema only for the steps that need them. Each milestone's brief names them explicitly.
- **Tier C (on-demand)** — glossary, risks, plan, current-state, journal. None preloaded. The agent greps or opens a specific section when a concrete problem demands it.

Typical saving: ~30% on M1–M4, ~20% on M5–M6 (where Tier B naturally grows). Frees context for the work that matters — reading Java classes from JAVA_REF and generating the .NET equivalents.

## Environment notes

- **Windows:** forward slashes in `{JAVA_REF}` (`C:/temp/...`). Most Powershell / Git-Bash / `dotnet` tooling accepts them and agent pipelines handle them consistently. Avoid single backslashes.
- **Linux / macOS:** `/tmp/aion-refs/4.6.2` disappears on reboot / periodic cleanup. For durable work, pick `~/work/aion-refs/4.6.2`.
- **WSL:** pick one side. Running the agent from WSL → Linux-style path. From Windows → Windows path. Mixing causes permission surprises.

## Complementary invocations

- Exploration only: append `Do not modify any file under AionLightning.NET/. Report findings only.` Useful for dry-runs or pre-sub-M inventory.
- Resume after blocker: the prompt works unchanged — the agent reads the journal, sees the last Step, continues. It must NOT re-do earlier Steps.
- Supervision: single session per milestone. Spinning up a fresh agent for every Step loses journal context.

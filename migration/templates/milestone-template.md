# M? — <Name>

Status: `[ ]` not started · Requires: M? · ADRs: 00?, 00?

## Goal

<One sentence: what this milestone demonstrates.>

## Scope

### In

- <Functionality included.>

### Out of scope

- <Explicitly deferred to a later milestone.>

## Dependencies

- Previous milestones: <list>
- ADRs activated: <list>
- Open questions / risks that must be closed before start: <list>

## Java source inventory

| Java package / file | Size (LOC) | .NET target |
|---|---|---|
| `AL-*/src/com/aionemu/.../X.java` | ? | `AionLightning.*/Y.cs` |

Inventory is taken at milestone start via `find AL-* -path "..." -name "*.java"`.

## .NET target layout

### New files

- `AionLightning.Commons/<Subsystem>/<File>.cs`
- ...

### Rewritten files

- `AionLightning.Login/<File>.cs` — current state in [`../current-state.md`](../current-state.md)
- ...

### Files removed

- `AionLightning.Commons/<File>.cs` — reason

## Definition of Done

- [ ] DoD criterion 1
- [ ] DoD criterion 2
- [ ] Solution builds, smoke scenario passes

## Smoke scenario

<Concrete manual verification: commands, steps, expected outcome.>

## Scoped risks

- <Risks specific to this milestone. If global — link to `../risks.md#r-N`.>

## Notes

<Details, examples, links to concrete Java files with line numbers.>

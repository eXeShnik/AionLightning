# ADR-005: Callbacks / event handling

- **Status:** Accepted (bus implementation finalised 2026-04-23)
- **Date:** 2026-04-23

## Context

The Java Aion codebase implements cross-cutting hooks (skill effects, quest triggers, state changes, logging, anti-cheat) using a Java-agent + ASM bytecode rewriting framework. `com.aionemu.commons.callbacks` defines `@ObjectCallback` / `@GlobalCallback` attributes; an agent rewrites annotated methods at class load to insert `Callbacks.runBefore(...)` / `runAfter(...)` calls.

This lets scripts / handlers subscribe to method-level events without editing the method's source.

The current .NET port in `AionLightning.Commons/Callbacks/` is a placeholder:

- `DotNetAgentEnhancer.Initialize()` = `Console.WriteLine("DotNetAgentEnhancer needs to be implemented using an AOP framework like Castle.Core.")` (`DotNetAgentEnhancer.cs:16-25`).
- `Enhancer/`, `Metadata/`, `Util/` — empty skeletons.

No caller in the .NET port actually uses this subsystem. It is pure dead weight.

## Decision

**Drop the AOP concept entirely. Use an explicit event bus backed by `System.Threading.Channels`.** Where the Java source has a `@ObjectCallback`-annotated method, the .NET port writes an explicit `await bus.PublishAsync(new XxxEvent(...), ct)` at the same call site.

### Bus implementation — bespoke `Channel<T>`-based

Final decision made at ADR acceptance time (previously open as Q-3).

```csharp
// AionLightning.Commons/Events/IEventBus.cs
public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IGameEvent;
}

public interface IGameEvent;

public interface IEventHandler<in TEvent> where TEvent : IGameEvent
{
    ValueTask HandleAsync(TEvent @event, CancellationToken ct);
}
```

Implementation outline:

```csharp
public sealed class InMemoryEventBus(IServiceProvider sp, ILogger<InMemoryEventBus> log) : IEventBus
{
    public async ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
        where TEvent : IGameEvent
    {
        var handlers = sp.GetServices<IEventHandler<TEvent>>();
        foreach (var h in handlers)
        {
            try
            {
                await h.HandleAsync(@event, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Handler {Handler} failed for {Event}", h.GetType().Name, typeof(TEvent).Name);
            }
        }
    }
}
```

Dispatch semantics:

- **Synchronous fan-out within a single `PublishAsync`** — all handlers are awaited before the method returns. Matches `@ObjectCallback` before/after semantics.
- **One handler's failure does not stop siblings** — logged per handler.
- **`OperationCanceledException` bubbles** — a cancelled publish stops cleanly.
- **Deferred queue mode (optional, M6+)** — a `Channel<object>`-backed variant delivers events asynchronously when ordering relaxations are desired (e.g. non-blocking broadcast). Not implemented in M5; interface stays the same.

### Usage pattern

```csharp
// .NET equivalent of an @ObjectCallback on Player.setHp
public async Task SetHpAsync(int newHp, CancellationToken ct)
{
    var oldHp = _hp;
    await _bus.PublishAsync(new HpChangingEvent(this, oldHp, newHp), ct);
    _hp = newHp;
    await _bus.PublishAsync(new HpChangedEvent(this, oldHp, newHp), ct);
}
```

Event type definition:

```csharp
public sealed record HpChangedEvent(IPlayer Player, int OldHp, int NewHp) : IGameEvent;
```

Handler registration:

```csharp
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<IEventHandler<HpChangedEvent>, DamageLogHandler>();
builder.Services.AddSingleton<IEventHandler<HpChangedEvent>, SurviveQuestHandler>();
```

## Alternatives considered

### Alt-1: MediatR

- Pros: mature library, standard `INotificationHandler<T>` contract.
- Cons: **paid license since v12 (Sept 2024)** for commercial use; runtime reflection on every publish to resolve handlers; attribute-based discovery adds complexity.
- Why rejected: licensing + performance. For a tick-rate event path (potentially thousands of publishes per second across AI / combat / movement) the reflection overhead and the licensing change are both blockers.

### Alt-2: Compile-time source generators

- Pros: attribute-driven, close to Java ergonomics, zero runtime reflection.
- Cons: writing a source generator is a non-trivial sub-project; diagnosability suffers (the code you read is not the code that runs); tooling support in Rider / VS still has rough edges.
- Why rejected: the cost of authoring the generator exceeds the cost of adding explicit `Publish` calls. Explicit calls are also easier to grep, debug and step through.

### Alt-3: Castle.Core DynamicProxy

- Pros: runtime proxy generation; smallest setup effort for AOP-style interception.
- Cons: requires `virtual` methods or interfaces on every target; per-call overhead is significant for a tick loop that mutates state 20+ times per second across thousands of entities; no Native AOT support.
- Why rejected: performance penalty is wrong for game state mutation; architectural cost (everything must be `virtual` or interface-backed) is invasive.

### Alt-4: C# 14 Interceptors

- Pros: official compile-time feature.
- Cons: targets specific call sites by file/line; designed for framework authors, not for general AOP. Breaks on refactor.
- Why rejected: not a general-purpose AOP tool.

### Alt-5: IL rewriting (Fody, PostSharp)

- Pros: closest analogue to the Java-agent approach.
- Cons: build-time toolchain overhead; not trimming-/AOT-friendly; PostSharp is a paid product.
- Why rejected: bigger disruption than writing `Publish` calls.

### Alt-6: Keep the concept, ship placeholders

- Pros: zero porting cost today.
- Cons: creates a real problem in M5 when callbacks actually matter; every Java method that relied on a callback needs a decision — it is cheaper to make that decision once, now, than ad-hoc later.
- Why rejected: procrastination.

## Consequences

### Positive

- Every event path is visible in code: grep for `Publish(new SkillCastEvent)` and you know every place that emits it.
- Debugging becomes trivial — ordinary breakpoints in handlers.
- No hidden proxies, no build-time magic, no per-call overhead beyond interface dispatch.
- Scripts subscribe through the same bus (reuses ADR-006 integration).
- Zero external licensing.

### Negative / trade-offs

- Mechanical cost: the Game module (M5–M6) has many sites where Java used `@ObjectCallback`. Each gains an explicit `Publish` call. One-time work on port.
- Slightly more verbose call sites.
- Ordering guarantees are the bus's responsibility — documented above (sync in-process fan-out).

## Implementation notes

- Target files (created in M5, interface declared in M1):
  - `AionLightning.Commons/Events/IEventBus.cs`
  - `AionLightning.Commons/Events/IEventHandler.cs`
  - `AionLightning.Commons/Events/IGameEvent.cs` (marker interface)
  - `AionLightning.Commons/Events/InMemoryEventBus.cs`
- DI extension: `services.AddAionEventBus()` wires up `IEventBus` as a singleton and registers all `IEventHandler<T>` closed types via assembly scanning (`services.Scan(...)` via `Scrutor`, optional — or hand-registered in M5).
- Event types live under the module that owns them: `AionLightning.Game.Events/*`, `AionLightning.Login.Events/*`.
- Naming: every event is `sealed record NameEvent(...) : IGameEvent`. Past tense for notifications (`HpChangedEvent`), gerund for pre-events (`HpChangingEvent`).

## Affected milestones

- M1: `IEventBus` / `IEventHandler<T>` / `IGameEvent` interfaces declared in Commons. No implementations.
- M2–M4: not used. Interface exists but no publishers.
- M5: `InMemoryEventBus` implementation lands. First consumer is world-entry broadcasting.
- M6: every subsystem (AI, skills, items, quests) publishes and handles events through this bus.

## Removals

- Everything under `AionLightning.Commons/Callbacks/` is deleted. Namely: `DotNetAgentEnhancer.cs`, `CallbackResult.cs`, `ICallback.cs`, `ICallbackPriority.cs`, `IEnhancedObject.cs`, and the `Enhancer/`, `Metadata/`, `Util/` folders.

## Related

- ADR-006 (Scripting): scripts subscribe to bus events via host-side interfaces.
- Risk: [R-002](../risks.md).

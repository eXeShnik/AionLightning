using System.Collections.Concurrent;
using AionLightning.Game.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>Per-tick callback context handed to a slot's OnTick/OnStop delegate.</summary>
public readonly record struct TickContext(Creature Effected, Creature Effector, AbnormalState Effect, int SkillId);

/// <summary>
/// Central, cancellable scheduler for DoT/HoT/drain-style periodic effect ticks. Replaces per-cast
/// fire-and-forget <c>Task.Run</c> tick loops so effect removal (dispel, expiry, death, logout) can
/// positively cancel outstanding ticks instead of relying on the loop noticing on its own next wake.
///
/// All tick-processing logic lives in <see cref="ProcessDueAsync"/> so it can be driven directly with
/// synthetic timestamps in tests; <see cref="ExecuteAsync"/> is just a <see cref="PeriodicTimer"/>
/// wrapper around it, matching the RegenService/NpcAiService BackgroundService pattern.
/// </summary>
public sealed class EffectTickScheduler : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(500);

    private sealed class Slot
    {
        public required int Handle;
        public required Creature Effected;
        public required Creature Effector;
        public required AbnormalState Effect;
        public required int SkillId;
        public required int IntervalMs;
        public required DateTime NextDueUtc;
        public required DateTime EndUtc;
        public required Func<TickContext, ValueTask> OnTick;
        public Func<TickContext, ValueTask>? OnStop;
        public volatile bool Cancelled;
        public bool Stopped;
    }

    private readonly ConcurrentDictionary<int, Slot> _slots = new();
    private int _nextHandle;
    private readonly ILogger<EffectTickScheduler> _log;

    /// <summary>Static access so non-DI domain objects (Creature) can reach the scheduler without a service locator.</summary>
    public static EffectTickScheduler? Instance { get; private set; }

    public EffectTickScheduler(ILogger<EffectTickScheduler> log)
    {
        _log = log;
        Instance = this;
    }

    /// <summary>Registers a periodic tick. Returns a handle that is also appended to <paramref name="effect"/>.TickHandles.</summary>
    public int Register(Creature effected, Creature effector, AbnormalState effect, int skillId,
        int intervalMs, DateTime endUtc, Func<TickContext, ValueTask> onTick, Func<TickContext, ValueTask>? onStop = null)
    {
        int handle = Interlocked.Increment(ref _nextHandle);
        var slot = new Slot
        {
            Handle     = handle,
            Effected   = effected,
            Effector   = effector,
            Effect     = effect,
            SkillId    = skillId,
            IntervalMs = intervalMs,
            NextDueUtc = DateTime.UtcNow.AddMilliseconds(intervalMs),
            EndUtc     = endUtc,
            OnTick     = onTick,
            OnStop     = onStop,
        };
        _slots[handle] = slot;
        (effect.TickHandles ??= new List<int>()).Add(handle);
        return handle;
    }

    /// <summary>Idempotent: marks the slot cancelled. ProcessDueAsync runs OnStop once and drops it.</summary>
    public void Cancel(int handle)
    {
        if (_slots.TryGetValue(handle, out var slot))
            slot.Cancelled = true;
    }

    public void CancelForEffect(AbnormalState effect)
    {
        if (effect.TickHandles is not { } handles) return;
        foreach (var handle in handles)
            Cancel(handle);
    }

    public void CancelAllFor(Creature creature)
    {
        foreach (var slot in _slots.Values)
            if (ReferenceEquals(slot.Effected, creature))
                slot.Cancelled = true;
    }

    /// <summary>
    /// Advances every registered slot against <paramref name="nowUtc"/>. Exposed internally so tests can
    /// drive it with synthetic time instead of waiting on the real timer.
    /// </summary>
    internal async ValueTask ProcessDueAsync(DateTime nowUtc)
    {
        foreach (var slot in _slots.Values.ToArray())
        {
            var ctx = new TickContext(slot.Effected, slot.Effector, slot.Effect, slot.SkillId);

            if (slot.Cancelled)
            {
                _slots.TryRemove(slot.Handle, out _);
                await StopOnceAsync(slot, ctx);
                continue;
            }

            if (nowUtc >= slot.EndUtc)
            {
                _slots.TryRemove(slot.Handle, out _);
                await StopOnceAsync(slot, ctx);
                continue;
            }

            if (slot.Effected.IsAlreadyDead)
            {
                _slots.TryRemove(slot.Handle, out _);
                await StopOnceAsync(slot, ctx);
                continue;
            }

            // Dispel fix: the effect was already removed from the creature's active list (dispel, cleanse,
            // etc.) by whichever code path removed it, which already ran its own cleanup — do NOT run OnStop
            // again here, just drop the slot.
            if (!slot.Effected.GetActiveEffects().Contains(slot.Effect))
            {
                _slots.TryRemove(slot.Handle, out _);
                continue;
            }

            if (nowUtc >= slot.NextDueUtc)
            {
                try { await slot.OnTick(ctx); }
                catch (Exception ex)
                {
                    _log.LogError(ex, "EffectTickScheduler: OnTick threw for skill {SkillId} (handle {Handle})", slot.SkillId, slot.Handle);
                }
                slot.NextDueUtc = slot.NextDueUtc.AddMilliseconds(slot.IntervalMs);
            }
        }
    }

    private async ValueTask StopOnceAsync(Slot slot, TickContext ctx)
    {
        if (slot.Stopped) return;
        slot.Stopped = true;
        if (slot.OnStop is null) return;
        try { await slot.OnStop(ctx); }
        catch (Exception ex)
        {
            _log.LogError(ex, "EffectTickScheduler: OnStop threw for skill {SkillId} (handle {Handle})", slot.SkillId, slot.Handle);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("EffectTickScheduler started (500ms tick)");
        using var timer = new PeriodicTimer(TickInterval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await ProcessDueAsync(DateTime.UtcNow);
        }
    }
}

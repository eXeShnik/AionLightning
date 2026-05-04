using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;

namespace AionLightning.Game.Combat;

/// <summary>
/// Single canonical damage-application point. Replaces inline
/// <c>target.CurrentHp = Math.Max(0, target.CurrentHp - damage)</c>
/// + <c>LastCombatTime</c> updates at the 14 damage sites and publishes
/// <see cref="DamageDealtEvent"/> for handler subscribers (M260+).
/// </summary>
public static class CreatureDamageExtensions
{
    public static async ValueTask ApplyDamageAndPublishAsync(
        this Creature target,
        Creature      attacker,
        int           damage,
        DamageKind    kind,
        int?          skillId,
        IEventBus     bus,
        CancellationToken ct = default,
        bool          suppressDeathEvent = false)
    {
        if (damage <= 0) return;

        // M265: pre-damage event — handlers (Shield/Protect) may mutate the cell to reduce damage
        var cell = new MutableDamage(damage);
        await bus.PublishAsync(new DamageReceivingEvent(attacker, target, cell, kind, skillId), ct);
        int finalDmg = Math.Max(0, cell.Value);
        if (finalDmg <= 0) return; // fully absorbed — skip HP write + post-event entirely

        // M287: capture liveness BEFORE the HP write so we can detect the alive→dead transition exactly once
        bool wasAlive           = target.CurrentHp > 0;

        target.CurrentHp        = Math.Max(0, target.CurrentHp - finalDmg);
        var now                 = DateTime.UtcNow;
        target.LastCombatTime   = now;
        attacker.LastCombatTime = now;

        await bus.PublishAsync(new DamageDealtEvent(attacker, target, finalDmg, kind, skillId), ct);

        // M287: publish DeathEvent exactly on the alive→dead edge; caller can suppress for duel-restore paths
        if (!suppressDeathEvent && wasAlive && target.CurrentHp == 0)
            await bus.PublishAsync(new DeathEvent(attacker, target, kind, skillId), ct);
    }
}

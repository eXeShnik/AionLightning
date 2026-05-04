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
        CancellationToken ct = default)
    {
        if (damage <= 0) return;

        target.CurrentHp        = Math.Max(0, target.CurrentHp - damage);
        var now                 = DateTime.UtcNow;
        target.LastCombatTime   = now;
        attacker.LastCombatTime = now;

        await bus.PublishAsync(new DamageDealtEvent(attacker, target, damage, kind, skillId), ct);
    }
}

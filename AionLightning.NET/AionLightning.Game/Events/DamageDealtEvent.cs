using AionLightning.Commons.Events;
using AionLightning.Game.Model;

namespace AionLightning.Game.Events;

/// <summary>
/// Source of a damage event — used by handlers to scope their reactions
/// (e.g. counter-attack only on auto-attack hits, not DoT ticks).
/// </summary>
public enum DamageKind
{
    AutoAttack,
    PhysicalSkill,
    MagicalSkill,
    Splash,
    DoTTick,
}

/// <summary>
/// Published after a Creature.CurrentHp mutation in a damage-application path.
/// Handlers (HealCastorOnAttacked, MagicCounterAtk, etc.) inspect target's active effects
/// and the attacker to react. Fire-and-forget — handler exceptions are swallowed by InMemoryEventBus.
/// </summary>
public sealed record DamageDealtEvent(
    Creature Attacker,
    Creature Target,
    int      DamageAmount,
    DamageKind Kind,
    int?     SkillId
) : IGameEvent;

/// <summary>
/// M265: Mutable damage cell so DamageReceivingEvent handlers (Shield, ProtectEffect, etc.)
/// can reduce or zero the incoming damage BEFORE it's applied to target.CurrentHp.
/// </summary>
public sealed class MutableDamage
{
    public int Value { get; set; }
    public MutableDamage(int initial) => Value = initial;
}

/// <summary>
/// Published BEFORE a Creature.CurrentHp mutation. Handlers may mutate <see cref="MutableDamage.Value"/>
/// to reduce/absorb the incoming damage. Handlers that absorb should also broadcast their own
/// shield-status packet (the canonical damage broadcast happens later from the caller).
/// </summary>
public sealed record DamageReceivingEvent(
    Creature      Attacker,
    Creature      Target,
    MutableDamage Damage,
    DamageKind    Kind,
    int?          SkillId
) : IGameEvent;

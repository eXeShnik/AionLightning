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

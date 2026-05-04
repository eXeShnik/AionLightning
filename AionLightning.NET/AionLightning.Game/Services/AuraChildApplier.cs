using AionLightning.Commons.Events;
using AionLightning.Game.Combat;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// M286c: applies an aura's child skill template to a single target each tick.
/// Recursion-guarded — if the child skill itself has &lt;aura&gt;, the aura branch is skipped
/// (heal/damage still apply but no nested tick spawns).
/// Routes child-skill damage through <see cref="CreatureDamageExtensions.ApplyDamageAndPublishAsync"/>
/// so the M260+ observer chain (Shield/Reflector/etc.) still fires.
/// </summary>
public sealed class AuraChildApplier
{
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<AuraChildApplier> _log;

    public AuraChildApplier(PlayerConnectionRegistry connRegistry, ILogger<AuraChildApplier> log)
    {
        _connRegistry = connRegistry;
        _log          = log;
    }

    public async ValueTask ApplyAsync(Player caster, Creature target, SkillTemplate child,
        IEventBus bus, CancellationToken ct = default)
    {
        if (target.IsAlreadyDead) return;
        if (child.Effects is null) return;

        // Recursion guard: nested aura → log + skip aura branch only (heal/damage still apply)
        if (child.Effects.HasAura)
            _log.LogDebug("Aura tick: nested <aura> on child skill {ChildId} ignored (depth cap=1)", child.SkillId);

        // Heal branch
        if (child.Effects.HealEffects is { Count: > 0 } healList)
        {
            int worldId = target.Position.WorldId;
            foreach (var he in healList)
            {
                int amount = he.BaseValue + he.Delta * Math.Max(1, child.SkillLevelOrDefault());
                if (he.IsPercent) amount = he.HealType switch
                {
                    "hp" => target.MaxHp * amount / 100,
                    "mp" => target.MaxMp * amount / 100,
                    "fp" => target is Player fpT ? fpT.MaxFp * amount / 100 : 0,
                    _    => amount,
                };
                if (amount <= 0) continue;

                if (he.HealType == "hp")
                {
                    int actual = Math.Min(amount, target.MaxHp - target.CurrentHp);
                    if (actual <= 0) continue;
                    target.CurrentHp += actual;
                    var pkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.NaturalHp, child.SkillId, actual, SM_ATTACK_STATUS.LogId.Heal);
                    foreach (var c in _connRegistry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == worldId)
                            try { await c.SendAsync(pkt, ct); } catch { }
                }
                else if (he.HealType == "mp")
                {
                    int actual = Math.Min(amount, target.MaxMp - target.CurrentMp);
                    if (actual <= 0) continue;
                    target.CurrentMp += actual;
                    var pkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.NaturalMp, child.SkillId, actual, SM_ATTACK_STATUS.LogId.MpHeal);
                    foreach (var c in _connRegistry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == worldId)
                            try { await c.SendAsync(pkt, ct); } catch { }
                }
                // FP/DP/VP heal: skipped to keep aura tick scope narrow
            }
        }

        // Damage branch — must route through ApplyDamageAndPublishAsync so observers (Shield/Reflector/etc.) fire
        if (child.Effects.DamageEffects is { Count: > 0 } dmgList)
        {
            var dmgFx = dmgList[0];
            int amount = dmgFx.BaseValue + dmgFx.Delta * Math.Max(1, child.SkillLevelOrDefault());
            if (amount > 0)
            {
                bool magical = dmgFx.DamageType != "physical";
                var kind = magical ? DamageKind.MagicalSkill : DamageKind.PhysicalSkill;
                await target.ApplyDamageAndPublishAsync(caster, amount, kind, child.SkillId, bus, ct);
            }
        }
    }
}

internal static class SkillTemplateAuraExtensions
{
    /// <summary>Aura ticks always evaluate at level 1 of the child skill (Java AuraEffect doesn't scale child by caster level).</summary>
    public static int SkillLevelOrDefault(this SkillTemplate _) => 1;
}

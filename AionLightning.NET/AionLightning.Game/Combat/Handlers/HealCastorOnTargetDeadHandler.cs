using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M287c: Handles &lt;healcastorontargetdead&gt; — when a creature carrying this buff dies, the
/// buff caster (effector) is healed if within Range. Optional healparty extends to caster's party.
///
/// Attribution per Java HealCastorOnTargetDeadEffect.java:
///   - effector = buff caster (Player) — heal recipient
///   - effected = the creature receiving the buff — the dying victim
///   - Observer attaches to effected (line 101); fires on its DEATH (line 66)
///   - Therefore: iterate VICTIM's active effects, look up the effector by ab.EffectorId, heal that effector.
///
/// Party scope: healparty="true" extends heal to caster's PlayerGroup members within range.
/// </summary>
public sealed class HealCastorOnTargetDeadHandler(
    IDataManager             dataManager,
    GameWorld                world,
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DeathEvent>
{
    public async ValueTask HandleAsync(DeathEvent e, CancellationToken ct)
    {
        var victim = e.Victim;
        var effects = victim.GetActiveEffects();
        if (effects.Count == 0) return;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            var fxList = tpl?.Effects?.HealOnTargetDeadEffects;
            if (fxList is not { Count: > 0 }) continue;

            // Resolve buff caster (effector). Heal recipient is the original buff caster, not the killer.
            var caster = world.GetPlayerByObjectId(ab.EffectorId);
            if (caster is null || caster.IsAlreadyDead) continue;
            if (!caster.Position.SameScope(victim.Position)) continue;

            foreach (var fx in fxList)
            {
                int healAmt = fx.BaseValue + fx.Delta * Math.Max(1, ab.SkillLevel);
                if (healAmt <= 0) continue;

                // Heal caster if within range of the dying creature
                if (fx.Range <= 0f || caster.Position.DistanceTo(victim.Position) <= fx.Range)
                    await ApplyHealAsync(caster, ab.SkillId, healAmt, fx.HealType, ct);

                // Party scope: extend heal to caster's group members within range of the victim
                if (fx.HealParty && caster.Group is { } grp)
                {
                    foreach (var member in grp.Members)
                    {
                        if (member.ObjectId == caster.ObjectId) continue;
                        if (member.IsAlreadyDead) continue;
                        if (!member.Position.SameScope(victim.Position)) continue;
                        if (fx.Range > 0f && member.Position.DistanceTo(victim.Position) > fx.Range) continue;
                        await ApplyHealAsync(member, ab.SkillId, healAmt, fx.HealType, ct);
                    }
                }
            }
        }
    }

    private async ValueTask ApplyHealAsync(Creature recipient, int skillId, int amount, string healType, CancellationToken ct)
    {
        if (healType == "mp")
        {
            int actual = Math.Min(amount, recipient.MaxMp - recipient.CurrentMp);
            if (actual <= 0) return;
            recipient.CurrentMp += actual;
            await BroadcastAsync(recipient, skillId, actual, SM_ATTACK_STATUS.AttackType.NaturalMp, SM_ATTACK_STATUS.LogId.MpHeal, ct);
        }
        else // hp default — Java types this as HP throughout the file
        {
            int actual = Math.Min(amount, recipient.MaxHp - recipient.CurrentHp);
            if (actual <= 0) return;
            recipient.CurrentHp += actual;
            await BroadcastAsync(recipient, skillId, actual, SM_ATTACK_STATUS.AttackType.NaturalHp, SM_ATTACK_STATUS.LogId.Heal, ct);
        }
    }

    private async ValueTask BroadcastAsync(Creature recipient, int skillId, int amount,
        SM_ATTACK_STATUS.AttackType atkType, SM_ATTACK_STATUS.LogId logId, CancellationToken ct)
    {
        var pkt = new SM_ATTACK_STATUS(recipient, atkType, skillId, amount, logId);
        var scope = recipient.Position;
        foreach (var c in connRegistry.GetAll())
            if (c.ActivePlayer is { } cp && cp.Position.SameScope(scope))
                try { await c.SendAsync(pkt, ct); } catch { }
    }
}

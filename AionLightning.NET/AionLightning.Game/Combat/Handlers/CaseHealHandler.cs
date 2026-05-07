using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M369: Handles &lt;caseheal&gt; via DamageDealtEvent (post-damage). When the buffed target receives damage
/// and their HP/MP is at or below cond_value% of max, applies a one-time heal and removes the buff.
/// Java analog: CaseHealEffect with ActionObserver(ObserverType.ATTACKED) + calculateHeal → effect.endEffect().
/// </summary>
public sealed class CaseHealHandler(
    IDataManager             dataManager,
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageDealtEvent>
{
    public async ValueTask HandleAsync(DamageDealtEvent e, CancellationToken ct)
    {
        var target = e.Target;
        if (target.IsAlreadyDead) return;

        var effects = target.GetActiveEffects();
        if (effects.Count == 0) return;

        foreach (var ab in effects)
        {
            var tpl    = dataManager.Skills.GetTemplate(ab.SkillId);
            var fxList = tpl?.Effects?.CaseHealEffects;
            if (fxList is not { Count: > 0 }) continue;

            foreach (var fx in fxList)
            {
                int current = fx.IsHp ? target.CurrentHp : target.CurrentMp;
                int max     = fx.IsHp ? target.MaxHp     : target.MaxMp;
                if (max <= 0) continue;

                if (current > max * fx.CondPercent / 100) continue;

                int healAmt = fx.IsPercent
                    ? max * (fx.Value + fx.Delta * Math.Max(1, ab.SkillLevel)) / 100
                    : fx.Value + fx.Delta * Math.Max(1, ab.SkillLevel);

                if (fx.IsHp)
                {
                    int actual = Math.Min(healAmt, target.MaxHp - target.CurrentHp);
                    if (actual > 0)
                    {
                        target.CurrentHp += actual;
                        await BroadcastAsync(target, ab.SkillId, actual,
                            SM_ATTACK_STATUS.AttackType.NaturalHp, SM_ATTACK_STATUS.LogId.Heal, ct);
                    }
                }
                else
                {
                    int actual = Math.Min(healAmt, target.MaxMp - target.CurrentMp);
                    if (actual > 0)
                    {
                        target.CurrentMp += actual;
                        await BroadcastAsync(target, ab.SkillId, actual,
                            SM_ATTACK_STATUS.AttackType.NaturalMp, SM_ATTACK_STATUS.LogId.MpHeal, ct);
                    }
                }

                // One-shot: remove the caseheal buff after it fires (mirrors Java effect.endEffect())
                target.RemoveEffectBySkillId(ab.SkillId);
                bool isPlayer = target is Player;
                var expPkt = new SM_ABNORMAL_EFFECT(target.ObjectId, isPlayer, target.GetActiveEffects());
                int worldId = target.Position.WorldId;
                foreach (var c in connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == worldId)
                        try { await c.SendAsync(expPkt, ct); } catch { }
                break; // only one caseheal buff fires per hit
            }
        }
    }

    private async ValueTask BroadcastAsync(Creature target, int skillId, int amount,
        SM_ATTACK_STATUS.AttackType atkType, SM_ATTACK_STATUS.LogId logId, CancellationToken ct)
    {
        var pkt     = new SM_ATTACK_STATUS(target, atkType, skillId, amount, logId);
        int worldId = target.Position.WorldId;
        foreach (var c in connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == worldId)
                try { await c.SendAsync(pkt, ct); } catch { }
    }
}

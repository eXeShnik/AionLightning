using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M264: Handles &lt;convertheal&gt; — when the buffed creature is hit, restore HealType
/// (HP or MP) by Value + Delta * SkillLevel. Java analog: ConvertHealEffect (AttackShieldObserver
/// pre-damage with damage→heal conversion). Our simplified post-damage approximation
/// applies an additive heal after the original hit lands.
/// </summary>
public sealed class ConvertHealHandler(
    IDataManager             dataManager,
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageDealtEvent>
{
    public async ValueTask HandleAsync(DamageDealtEvent e, CancellationToken ct)
    {
        if (e.Kind == DamageKind.DoTTick) return;

        var target = e.Target;
        if (target.IsAlreadyDead) return;

        var effects = target.GetActiveEffects();
        if (effects.Count == 0) return;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            var fxList = tpl?.Effects?.ConvertHealEffects;
            if (fxList is not { Count: > 0 }) continue;

            foreach (var fx in fxList)
            {
                int healAmt = fx.BaseValue + fx.Delta * Math.Max(1, ab.SkillLevel);
                if (healAmt <= 0) continue;

                if (fx.HealType == "hp")
                {
                    int actual = Math.Min(healAmt, target.MaxHp - target.CurrentHp);
                    if (actual <= 0) continue;
                    target.CurrentHp += actual;
                    await BroadcastAsync(target, ab.SkillId, actual, SM_ATTACK_STATUS.AttackType.NaturalHp, SM_ATTACK_STATUS.LogId.Heal, ct);
                }
                else // mp
                {
                    int actual = Math.Min(healAmt, target.MaxMp - target.CurrentMp);
                    if (actual <= 0) continue;
                    target.CurrentMp += actual;
                    await BroadcastAsync(target, ab.SkillId, actual, SM_ATTACK_STATUS.AttackType.NaturalMp, SM_ATTACK_STATUS.LogId.MpHeal, ct);
                }
            }
        }
    }

    private async ValueTask BroadcastAsync(Creature target, int skillId, int amount,
        SM_ATTACK_STATUS.AttackType atkType, SM_ATTACK_STATUS.LogId logId, CancellationToken ct)
    {
        var pkt = new SM_ATTACK_STATUS(target, atkType, skillId, amount, logId);
        int worldId = target.Position.WorldId;
        foreach (var c in connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == worldId)
                try { await c.SendAsync(pkt, ct); } catch { }
    }
}

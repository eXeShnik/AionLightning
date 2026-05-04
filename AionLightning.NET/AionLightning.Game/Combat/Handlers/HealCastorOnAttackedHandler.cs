using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M260: Handles &lt;healcastoronatk&gt; — when a creature carrying this buff is attacked,
/// the buff caster is healed by `value + delta * skillLevel` if within `range` of the target.
/// Java analog: HealCastorOnAttackedEffect (ActionObserver ATTACKED).
/// </summary>
public sealed class HealCastorOnAttackedHandler(
    IDataManager             dataManager,
    GameWorld                world,
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageDealtEvent>
{
    public async ValueTask HandleAsync(DamageDealtEvent e, CancellationToken ct)
    {
        var target = e.Target;
        if (target.IsAlreadyDead) return;

        // Snapshot to avoid mutation while iterating
        var effects = target.GetActiveEffects();
        if (effects.Count == 0) return;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            var fxList = tpl?.Effects?.HealCastorOnAtkEffects;
            if (fxList is not { Count: > 0 }) continue;

            // Caster is the creature that applied the buff
            var caster = world.GetPlayerByObjectId(ab.EffectorId)
                      ?? (Creature?)world.GetNpcByObjectId(ab.EffectorId);
            if (caster is null || caster.IsAlreadyDead) continue;
            if (caster.Position.WorldId != target.Position.WorldId) continue;

            foreach (var fx in fxList)
            {
                // Range gate (Java HealCastorOnAttackedEffect "radius" check)
                if (fx.Range > 0f && caster.Position.DistanceTo(target.Position) > fx.Range) continue;

                int healAmt = fx.BaseValue + fx.Delta * Math.Max(1, ab.SkillLevel);
                if (healAmt <= 0) continue;

                if (fx.HealType == "hp")
                {
                    int actual = Math.Min(healAmt, caster.MaxHp - caster.CurrentHp);
                    if (actual <= 0) continue;
                    caster.CurrentHp += actual;
                    await BroadcastHealAsync(caster, ab.SkillId, actual, SM_ATTACK_STATUS.AttackType.NaturalHp, SM_ATTACK_STATUS.LogId.Heal, ct);
                }
                else // mp
                {
                    int actual = Math.Min(healAmt, caster.MaxMp - caster.CurrentMp);
                    if (actual <= 0) continue;
                    caster.CurrentMp += actual;
                    await BroadcastHealAsync(caster, ab.SkillId, actual, SM_ATTACK_STATUS.AttackType.NaturalMp, SM_ATTACK_STATUS.LogId.MpHeal, ct);
                }
            }
        }
    }

    private async ValueTask BroadcastHealAsync(Creature caster, int skillId, int amount,
        SM_ATTACK_STATUS.AttackType atkType, SM_ATTACK_STATUS.LogId logId, CancellationToken ct)
    {
        var pkt = new SM_ATTACK_STATUS(caster, atkType, skillId, amount, logId);
        int worldId = caster.Position.WorldId;
        foreach (var c in connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == worldId)
                try { await c.SendAsync(pkt, ct); } catch { }
    }
}

using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M313: Handles &lt;alwaysblock&gt; and &lt;alwaysdodge&gt; — physical hits are guaranteed to be
/// blocked/dodged for up to HitCountRemaining hits; when the counter reaches 0 the buff is removed.
/// Java analog: AlwaysBlockEffect/AlwaysDodgeEffect use AttackStatusObserver to intercept each hit.
/// </summary>
public sealed class AlwaysBlockDodgeHandler(
    IDataManager             dataManager,
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageReceivingEvent>
{
    public async ValueTask HandleAsync(DamageReceivingEvent e, CancellationToken ct)
    {
        if (e.Damage.Value <= 0) return;
        if (e.Kind is not (DamageKind.PhysicalSkill or DamageKind.AutoAttack)) return;

        var target = e.Target;
        if (target.IsAlreadyDead) return;

        var effects = target.GetActiveEffects();
        if (effects.Count == 0) return;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            if (tpl?.Effects is null) continue;
            bool isBlock = tpl.Effects.HasAlwaysBlock;
            bool isDodge = tpl.Effects.HasAlwaysDodge;
            if (!isBlock && !isDodge) continue;

            e.Damage.Value = 0;
            ab.HitCountRemaining--;

            if (ab.HitCountRemaining <= 0)
            {
                target.RemoveEffectBySkillId(ab.SkillId);
                bool isPlayer = target is Player;
                var abnPkt = new SM_ABNORMAL_EFFECT(target.ObjectId, isPlayer, target.GetActiveEffects());
                int worldId = target.Position.WorldId;
                foreach (var c in connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == worldId)
                        try { await c.SendAsync(abnPkt, ct); } catch { }
            }
            return;
        }
    }
}

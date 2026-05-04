using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M276: Handles &lt;alwaysresist&gt; — when buffed creature carries this buff, MagicalSkill damage
/// is fully resisted (cell zeroed). Pairs with `<convertheal>` in the boss "Holy Fortitude" mechanic
/// where alwaysresist absorbs magical hits and convertheal uses physical hits to refill HP/MP.
/// Java analog: AlwaysResistEffect (related to AlwaysResistEffectTemplate / SkillElement filter).
/// </summary>
public sealed class AlwaysResistHandler(
    IDataManager             dataManager,
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageReceivingEvent>
{
    public async ValueTask HandleAsync(DamageReceivingEvent e, CancellationToken ct)
    {
        if (e.Damage.Value <= 0) return;
        if (e.Kind != DamageKind.MagicalSkill && e.Kind != DamageKind.DoTTick) return;

        var target = e.Target;
        if (target.IsAlreadyDead) return;

        var effects = target.GetActiveEffects();
        if (effects.Count == 0) return;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            if (tpl?.Effects?.HasAlwaysResist != true) continue;

            int absorbed = e.Damage.Value;
            e.Damage.Value = 0;

            var pkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage,
                ab.SkillId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
            int worldId = target.Position.WorldId;
            foreach (var c in connRegistry.GetAll())
                if (c.ActivePlayer?.Position.WorldId == worldId)
                    try { await c.SendAsync(pkt, ct); } catch { }
            return;
        }
    }
}

using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M268: Handles &lt;sanctuary&gt; — when the buffed creature carries any sanctuary buff,
/// incoming damage is fully absorbed (cell zeroed). Java SanctuaryEffect is a TODO stub; this
/// implementation provides the sensible default behavior: full immunity while sanctuary is active.
/// </summary>
public sealed class SanctuaryHandler(
    IDataManager             dataManager,
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageReceivingEvent>
{
    public async ValueTask HandleAsync(DamageReceivingEvent e, CancellationToken ct)
    {
        if (e.Damage.Value <= 0) return;

        var target = e.Target;
        if (target.IsAlreadyDead) return;

        var effects = target.GetActiveEffects();
        if (effects.Count == 0) return;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            if (tpl?.Effects?.HasSanctuary != true) continue;

            // Full immunity: zero the cell + broadcast a status packet so clients see the absorb
            int absorbed = e.Damage.Value;
            e.Damage.Value = 0;

            var pkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.ProtectDmg,
                ab.SkillId, absorbed, SM_ATTACK_STATUS.LogId.Regular);
            var scope = target.Position;
            foreach (var c in connRegistry.GetAll())
                if (c.ActivePlayer is { } cp && cp.Position.SameScope(scope))
                    try { await c.SendAsync(pkt, ct); } catch { }
            return;
        }
    }
}

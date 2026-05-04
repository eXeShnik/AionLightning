using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M265: Handles &lt;shield&gt; via DamageReceivingEvent (pre-damage). Absorbs
/// HitValue + HitDelta * SkillLevel damage per incoming hit by mutating
/// <see cref="MutableDamage.Value"/> downwards. Java analog: ShieldEffect with AttackShieldObserver.
/// </summary>
public sealed class ShieldHandler(
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
            if (e.Damage.Value <= 0) break; // already fully absorbed by an earlier shield

            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            var fxList = tpl?.Effects?.ShieldEffects;
            if (fxList is not { Count: > 0 }) continue;

            foreach (var fx in fxList)
            {
                if (e.Damage.Value <= 0) break;

                int absorbCap = fx.HitValue + fx.HitDelta * Math.Max(1, ab.SkillLevel);
                if (absorbCap <= 0) continue;

                int absorbed = Math.Min(e.Damage.Value, absorbCap);
                e.Damage.Value -= absorbed;

                // Broadcast a ProtectDmg-style status to the world so clients see the absorb
                var pkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.ProtectDmg,
                    ab.SkillId, absorbed, SM_ATTACK_STATUS.LogId.Regular);
                int worldId = target.Position.WorldId;
                foreach (var c in connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == worldId)
                        try { await c.SendAsync(pkt, ct); } catch { }
            }
        }
    }
}

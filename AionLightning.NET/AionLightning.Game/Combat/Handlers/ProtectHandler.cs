using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M266: Handles &lt;protect&gt; — redirects (HitValue% if IsPercent, else flat HitValue)
/// of incoming damage from the buffed creature to the buff caster.
/// Java analog: ProtectEffect (extends ShieldEffect, shieldType=8).
/// </summary>
public sealed class ProtectHandler(
    IDataManager             dataManager,
    GameWorld                world,
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
            if (e.Damage.Value <= 0) break;

            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            var fxList = tpl?.Effects?.ProtectEffects;
            if (fxList is not { Count: > 0 }) continue;

            var caster = world.GetPlayerByObjectId(ab.EffectorId)
                      ?? (Creature?)world.GetNpcByObjectId(ab.EffectorId);
            if (caster is null || caster.IsAlreadyDead) continue;
            if (!caster.Position.SameScope(target.Position)) continue;

            foreach (var fx in fxList)
            {
                if (e.Damage.Value <= 0) break;
                if (fx.HitValue <= 0) continue;
                if (fx.Radius > 0f && caster.Position.DistanceTo(target.Position) > fx.Radius) continue;

                int redirect = fx.IsPercent
                    ? Math.Max(1, e.Damage.Value * fx.HitValue / 100)
                    : Math.Min(fx.HitValue, e.Damage.Value);
                redirect = Math.Min(redirect, e.Damage.Value);
                if (redirect <= 0) continue;

                e.Damage.Value -= redirect;

                int casterDmg = Math.Min(redirect, caster.CurrentHp);
                caster.CurrentHp = Math.Max(0, caster.CurrentHp - casterDmg);

                var pkt = new SM_ATTACK_STATUS(caster, SM_ATTACK_STATUS.AttackType.Damage,
                    ab.SkillId, casterDmg, SM_ATTACK_STATUS.LogId.Regular);
                var scope = caster.Position;
                foreach (var c in connRegistry.GetAll())
                    if (c.ActivePlayer is { } cp && cp.Position.SameScope(scope))
                        try { await c.SendAsync(pkt, ct); } catch { }
            }
        }
    }
}

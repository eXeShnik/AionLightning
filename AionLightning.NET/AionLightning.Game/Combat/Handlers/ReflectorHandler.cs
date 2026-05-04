using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M263: Handles &lt;reflector&gt; — when the buffed creature is hit, attacker takes
/// HitValue + HitDelta * SkillLevel damage back. Range-gated by Radius.
/// Java analog: ReflectorEffect (AttackShieldObserver pre-damage). Our simplified post-damage
/// approximation reflects equivalent damage AFTER the original hit lands.
/// </summary>
public sealed class ReflectorHandler(
    IDataManager             dataManager,
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageDealtEvent>
{
    public async ValueTask HandleAsync(DamageDealtEvent e, CancellationToken ct)
    {
        // DoT ticks shouldn't trigger reflect — Java's observer fires per attack, not per tick
        if (e.Kind == DamageKind.DoTTick) return;

        var target = e.Target;
        var attacker = e.Attacker;
        if (attacker.IsAlreadyDead) return;
        if (ReferenceEquals(attacker, target)) return; // self-damage skips reflect

        var effects = target.GetActiveEffects();
        if (effects.Count == 0) return;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            var fxList = tpl?.Effects?.ReflectorEffects;
            if (fxList is not { Count: > 0 }) continue;

            foreach (var fx in fxList)
            {
                if (fx.HitValue <= 0) continue;

                // Range gate: attacker must be within Radius of target for reflect to fire
                if (fx.Radius > 0f && attacker.Position.DistanceTo(target.Position) > fx.Radius) continue;

                int reflectDmg = fx.HitValue + fx.HitDelta * Math.Max(1, ab.SkillLevel);
                if (reflectDmg <= 0) continue;
                reflectDmg = Math.Min(reflectDmg, attacker.CurrentHp); // can't take below 0
                if (reflectDmg <= 0) continue;

                attacker.CurrentHp = Math.Max(0, attacker.CurrentHp - reflectDmg);

                var pkt = new SM_ATTACK_STATUS(attacker, SM_ATTACK_STATUS.AttackType.Damage,
                    ab.SkillId, reflectDmg, SM_ATTACK_STATUS.LogId.Regular);
                int worldId = attacker.Position.WorldId;
                foreach (var c in connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == worldId)
                        try { await c.SendAsync(pkt, ct); } catch { }
            }
        }
    }
}

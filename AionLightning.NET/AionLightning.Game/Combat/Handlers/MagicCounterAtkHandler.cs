using AionLightning.Commons.Events;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M261: Handles &lt;magiccounteratk&gt; — when the buffed creature casts a magical attack skill,
/// self-damage = min(MaxDmg, attacker.MaxHp * Percent / 100). "Blood magic" mechanic.
/// Java analog: MagicCounterAtkEffect (ActionObserver SKILLUSE; filters MAGICAL+ATTACK skill).
/// </summary>
public sealed class MagicCounterAtkHandler(
    IDataManager             dataManager,
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageDealtEvent>
{
    public async ValueTask HandleAsync(DamageDealtEvent e, CancellationToken ct)
    {
        if (e.Kind != DamageKind.MagicalSkill) return;

        var attacker = e.Attacker;
        if (attacker.IsAlreadyDead) return;

        var effects = attacker.GetActiveEffects();
        if (effects.Count == 0) return;

        foreach (var ab in effects)
        {
            var tpl = dataManager.Skills.GetTemplate(ab.SkillId);
            var fxList = tpl?.Effects?.MagicCounterAtkEffects;
            if (fxList is not { Count: > 0 }) continue;

            foreach (var fx in fxList)
            {
                if (fx.Percent <= 0) continue;
                int rawSelf = attacker.MaxHp * fx.Percent / 100;
                int selfDmg = fx.MaxDmg > 0 ? Math.Min(rawSelf, fx.MaxDmg) : rawSelf;
                if (selfDmg <= 0) continue;

                attacker.CurrentHp = Math.Max(0, attacker.CurrentHp - selfDmg);

                var pkt = new SM_ATTACK_STATUS(attacker, SM_ATTACK_STATUS.AttackType.Damage,
                    ab.SkillId, selfDmg, SM_ATTACK_STATUS.LogId.Regular);
                var scope = attacker.Position;
                foreach (var c in connRegistry.GetAll())
                    if (c.ActivePlayer is { } cp && cp.Position.SameScope(scope))
                        try { await c.SendAsync(pkt, ct); } catch { }
            }
        }
    }
}

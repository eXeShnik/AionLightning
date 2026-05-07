using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// M366: Handles &lt;mpshield&gt; via DamageReceivingEvent (pre-damage). Absorbed damage is drained
/// from the target's MP instead of HP. Delegates to Creature.TryAbsorbMpShield which tracks the
/// pool and removes the effect when exhausted.
/// Java analog: MpShieldEffect with AttackShieldObserver (shieldType=9).
/// </summary>
public sealed class MpShieldHandler(
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageReceivingEvent>
{
    public async ValueTask HandleAsync(DamageReceivingEvent e, CancellationToken ct)
    {
        if (e.Damage.Value <= 0) return;

        var target = e.Target;
        if (target.IsAlreadyDead) return;

        int original  = e.Damage.Value;
        int remaining = target.TryAbsorbMpShield(original, out int shieldSkillId);
        if (remaining == original) return; // no active mpshield or nothing absorbed

        e.Damage.Value = remaining;
        int absorbed   = original - remaining;

        var pkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.ProtectDmg,
            shieldSkillId, absorbed, SM_ATTACK_STATUS.LogId.Regular);
        int worldId = target.Position.WorldId;
        foreach (var c in connRegistry.GetAll())
            if (c.ActivePlayer?.Position.WorldId == worldId)
                try { await c.SendAsync(pkt, ct); } catch { }
    }
}

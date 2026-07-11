using AionLightning.Commons.Events;
using AionLightning.Game.Events;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Combat.Handlers;

/// <summary>
/// Sends SM_STATUPDATE_HP to a player whose HP just changed from damage.
/// Java analog: PlayerLifeStats.onReduceHp → SM_STATUPDATE_HP to the owner.
/// </summary>
public sealed class HpUpdateHandler(
    PlayerConnectionRegistry connRegistry)
    : IEventHandler<DamageDealtEvent>
{
    public async ValueTask HandleAsync(DamageDealtEvent e, CancellationToken ct)
    {
        if (e.Target is not Player target) return;

        foreach (var c in connRegistry.GetAll())
        {
            if (c.ActivePlayer != target) continue;
            try { await c.SendAsync(new SM_STATUPDATE_HP(target.CurrentHp, target.MaxHp), ct); } catch { }
            break;
        }
    }
}

// SauroSupplyBasePortal — Java ai/portals/SauroSupplyBasePortal.java. Sauro supply-base entrance:
// requires a medal item count before allowing entry into the instance.
using System.Linq;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("sauro_entrance")]
public sealed class SauroSupplyBasePortal : ActionItemNpcAI2
{
    private const int EntryItem = 186000236;
    private const int EntryCount = 3;

    protected override void HandleUseItemFinish(Player player)
    {
        long owned = player.Inventory.All.Where(i => i.ItemId == EntryItem).Sum(i => i.Count);
        if (owned >= EntryCount)
        {
            // note: Java teleported to world 301130000 via TeleportService2.teleportTo, resolving or
            // registering an instance channel through InstanceService first. Neither service has a
            // static injection point on NpcAi2, so the actual teleport is a no-op here.
        }
        else
        {
            // note: Java sent a raw chat message via PacketSendUtility.sendMessage — not reachable
            // from the script layer.
        }
    }
}

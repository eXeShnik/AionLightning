// ShugoRelicsAI2 — Java ai/instance/shugoImperialTomb/ShugoRelicsAI2.java. Shugo Imperial Tomb relic
// chest: consumes 1-3 key items then registers a group/alliance-scoped drop.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("shugo_relic")]
// 831122, 831123, 831124, 831373
public sealed class ShugoRelicsAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java validated key-item consumption (1 key for 831122/831123/831124, 3 keys for
        // 831373; item 185000129 or 185000128) via Inventory.decreaseByItemId, sent a system/chat
        // message when the player lacked keys, and otherwise silently killed itself and registered a
        // group/alliance-scoped drop (DropRegistrationService/DropService) for every in-range member.
        // Inventory item consumption, drop registration, and AuditLogger aren't exposed to scripts yet.
        base.HandleUseItemFinish(player);
    }
}

// ChestAI2 — Java ai/ChestAI2.java. Lootable chest: validates key items, registers a group/
// alliance-scoped drop, then opens the loot list.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("chest")]
public sealed class ChestAI2 : ActionItemNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java looked up a ChestTemplate (DataManager.CHEST_DATA) and bailed out if none was found
        // before running the use-item flow; chest templates aren't in IDataManager yet.
        base.OnDialogStart(player);
    }

    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java validated the chest's key items (analyzeOpening), registered a group/alliance-scoped
        // drop via DropRegistrationService/DropService, and audit-logged double-loot attempts. Chest key
        // templates, drop registration, and AuditLogger aren't wired at the script layer yet.
        base.HandleUseItemFinish(player);
    }

    public override void OnDialogFinish(Player player)
    {
    }
}

// KromedesItemNpcsAI2 — Java ai/instance/kromedesTrial/KromedesItemNpcsAI2.java. Use-item NPCs
// that also grant a per-npcId quest item once on first dialog select.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("krobject")]
public sealed class KromedesItemNpcsAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java sent SM_DIALOG_WINDOW(objectId, 1011) here; PacketSendUtility isn't wired at the
        // script layer yet.
    }

    // note: Java's onDialogSelect (dropped — no C# equivalent hook) granted a per-npcId reward item
    // (730325 -> 164000142, 730340 -> 164000140, 730341 -> 164000143) the first time dialogId 1012
    // was selected, or re-showed page 27 once already granted. ItemService/SM_DIALOG_WINDOW aren't
    // exposed to the script layer yet.
}

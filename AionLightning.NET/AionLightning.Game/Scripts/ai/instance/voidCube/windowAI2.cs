// windowAI2 — Java ai/instance/voidCube/windowAI2.java. Void Cube "window": consumes a color-specific
// bomb item from the interacting player's inventory to break the corresponding gate window.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("window")]
public sealed class windowAI2 : ActionItemNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java checked the player's inventory for a color-specific bomb item (164000271/272/273
        // keyed by npcId 701581/582/583), consumed it and killed the owner on success, or sent a
        // bright-yellow system message otherwise; inventory item-count/consume and colored system
        // messages aren't wired at the script layer yet.
    }

    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java called AI2Actions.deleteOwner(this) here; no C# equivalent delete-owner action exists.
    }
}

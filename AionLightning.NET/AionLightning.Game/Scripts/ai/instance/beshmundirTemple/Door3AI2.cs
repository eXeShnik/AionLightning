// Door3AI2 — Java ai/instance/beshmundirTemple/Door3AI2.java. Instance door gated on possession of a
// key item.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("door3")]
public sealed class Door3AI2 : ActionItemNpcAI2
{
    private const int KeyItemId = 185000091;

    public override void OnDialogStart(Player player)
    {
        // note: Java rejected entry with a bare SM_DIALOG_WINDOW when the key item was missing; that
        // packet isn't wired at the script layer yet, so failing the check below has no observable effect
        // here. FindByItemId checks presence in the bag, not the summed stack count Java's call used.
        if (player.Inventory.FindByItemId(KeyItemId) is not null)
        {
            base.OnDialogStart(player);
        }
    }

    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java deleted the door via AI2Actions.deleteOwner; no scripted despawn API exists yet.
    }
}

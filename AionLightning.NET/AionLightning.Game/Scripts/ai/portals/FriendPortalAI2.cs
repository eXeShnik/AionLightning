// FriendPortalAI2 — Java ai/portals/FriendPortalAI2.java. Housing "friend portal" summon: lets the
// house owner, their friends, or legion mates open the housing friend-list teleport dialog.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("friendportal")]
public sealed class FriendPortalAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java cast the owner to SummonedHouseNpc and checked player.getFriendList()/
        // getLegion().isMember() against the house-npc creator's owner id, then opened
        // DialogPage.HOUSING_FRIENDLIST or sent STR_HOUSING_TELEPORT_CANT_USE. SummonedHouseNpc,
        // Player's friend list and SM_DIALOG_WINDOW aren't reachable from the script layer.
    }
}

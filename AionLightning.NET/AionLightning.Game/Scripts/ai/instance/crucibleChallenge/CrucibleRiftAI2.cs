// CrucibleRiftAI2 — Java ai/instance/crucibleChallenge/CrucibleRiftAI2.java. Crucible Challenge
// portal prop: announces itself on spawn, then either opens a confirmation dialog or teleports the
// player through the rift depending on variant.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

// note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — dialogId 10000, npcId
// 730459 — teleported the player through the rift, spawned three follow-up props, and deleted itself
// via AI2Actions.deleteOwner; that hook has no equivalent on NpcAi2, and TeleportService2 isn't exposed
// to scripts yet.
[AiName("cruciblerift")]
public sealed class CrucibleRiftAI2 : ActionItemNpcAI2
{
    public override void OnSpawned()
    {
        base.OnSpawned();
        if (getOwner().Template.NpcId == 730459)
        {
            // note: Java broadcast SM_SYSTEM_MESSAGE 1111482 to every player in the instance via
            // WorldMapInstance.doOnAllPlayers; instance-wide player broadcast isn't exposed to scripts yet.
        }
    }

    protected override void HandleUseItemFinish(Player player)
    {
        switch (getOwner().Template.NpcId)
        {
            case 730459:
                // note: Java opened a confirmation dialog (SM_DIALOG_WINDOW); not exposed to scripts yet.
                break;
            case 730460:
                // note: Java teleported the player through the rift (TeleportService2), spawned a follow-up
                // prop (205679), and deleted itself via AI2Actions.deleteOwner; none of these are exposed
                // to scripts yet.
                break;
        }
    }
}

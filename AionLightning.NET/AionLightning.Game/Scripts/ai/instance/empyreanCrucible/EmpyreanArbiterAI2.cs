// EmpyreanArbiterAI2 — Java ai/instance/empyreanCrucible/EmpyreanArbiterAI2.java. Stage-gate NPC that
// teleports a player onward once they present the required key item.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("empyreanarbiter")]
public sealed class EmpyreanArbiterAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java checked the player's inventory for item 186000124 and opened dialog page 1011 (or
        // page 0 as a fallback); Player inventory queries and SM_DIALOG_WINDOW aren't exposed to scripts
        // yet. Java's onDialogSelect (dropped — no C# hook exists) consumed that item on dialog 10000,
        // teleported the player to a per-npc-id waypoint further into the crucible instance via
        // TeleportService2, reset the instance's CruciblePlayerReward.playerDefeated flag, and broadcast
        // a system message to every player in the instance.
    }
}

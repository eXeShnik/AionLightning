// KharunAI2 — Java ai/instance/tiamatStrongHold/KharunAI2.java. Dialog NPC that triggers the
// Kharun cutscene/event once a player confirms the prompt.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("kharun")]
public sealed class KharunAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java sent an SM_DIALOG_WINDOW(1011) packet to the player. PacketSendUtility and
        // SM_DIALOG_WINDOW aren't exposed to scripts yet.
    }

    // note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — dialogId 10000
    // deleted this owner (AI2Actions.deleteOwner) and started the Kharun event below. onDialogSelect
    // has no equivalent hook on NpcAi2, and NPC removal isn't exposed to scripts yet.

    private void StartKharunEvent()
    {
        ScheduleTask(() =>
        {
            Spawn(800335, Owner.Position.X, Owner.Position.Y, Owner.Position.Z);
            // note: Java also had the spawned Kharun target and cast skill 20943 on the instance's
            // aetheric field NPC (730613), shouted two lines via NpcShoutsService, killed the
            // stronghold door NPC (730612), and removed the aetheric field. Cross-NPC targeting,
            // NpcShoutsService, and NPC removal/death aren't exposed to scripts yet.
        }, 3000);
    }
}

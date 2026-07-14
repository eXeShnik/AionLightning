// AstricloxSeedAI2 — Java ai/worlds/sarpan/AstricloxSeedAI2.java. Flight-teleport dialog npc that
// self-despawns after 5 minutes.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("astricloxseed")]
public sealed class AstricloxSeedAI2 : NpcAi2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java opened SM_DIALOG_WINDOW(1011) here and, on dialogId 10000 in onDialogSelect, flight-
        // teleported the player (194001) via CreatureState + SM_EMOTION; dialog packets and onDialogSelect
        // (no matching NpcAi2 hook) aren't exposed to scripts yet.
    }

    public override void OnSpawned()
    {
        base.OnSpawned();
        ScheduleTask(() =>
        {
            // note: Java deleted its own owner here (getController().onDelete()) after a 5-minute delay;
            // no owner-delete hook is exposed to scripts yet.
        }, 60000 * 5);
    }
}

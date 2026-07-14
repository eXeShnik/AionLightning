// DaevaTeleportAI2 — Java ai/quests/DaevaTeleportAI2.java. Daeva teleporter NPC gated to level 10+.
using AionLightning.Game.Ai;

namespace Ai;

[AiName("daevateleporter")]
public sealed class DaevaTeleportAI2 : GeneralNpcAI2
{
    // note: Java's onDialogSelect blocked players below level 10 with an SM_DIALOG_WINDOW(NO_RIGHT)
    // popup, otherwise falling through to normal dialog handling. onDialogSelect has no NpcAi2 hook to
    // override, and SM_DIALOG_WINDOW isn't reachable from the script layer.
}

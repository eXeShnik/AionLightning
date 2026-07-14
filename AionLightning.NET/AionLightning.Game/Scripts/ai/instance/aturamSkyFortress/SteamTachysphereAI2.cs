// SteamTachysphereAI2 — Java ai/instance/aturamSkyFortress/SteamTachysphereAI2.java. Quest-gated
// item-use prop: on use, checks a race-specific quest's status and, once complete, teleports the
// player out and plays a cutscene.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("steam_tachysphere")]
public sealed class SteamTachysphereAI2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java looked up the player's race-specific quest (18302 Elyos / 28302 Asmodian) and sent
        // an SM_DIALOG_WINDOW variant depending on whether it existed and was complete. QuestState
        // lookup and SM_DIALOG_WINDOW aren't exposed to scripts yet.
    }

    // note: Java's onDialogSelect(player, dialogId, questId, extendedRewardIndex) — dialogId 10000, once
    // the quest was confirmed complete, teleported the player to map 300240000, played a cutscene
    // (SM_PLAY_MOVIE), stopped their protection task, and cast skill 19502 on them — has no equivalent
    // hook on NpcAi2, and TeleportService2/SM_PLAY_MOVIE/protection-task control aren't exposed to
    // scripts yet.
}

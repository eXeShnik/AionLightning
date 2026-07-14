// HolyAltarAI2 — Java ai/instance/shugoImperialTomb/HolyAltarAI2.java. Shugo Imperial Tomb altar:
// opens a quest-state-dependent dialog window, and teleports players into treasure rooms after
// consuming a key item.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("holy_altar")]
// 831350
public sealed class HolyAltarAI2 : GeneralNpcAI2
{
    public override void OnDialogStart(Player player)
    {
        // note: Java resolved this npc's on-talk quests (QuestEngine.getQuestNpc) and opened one of four
        // SM_DIALOG_WINDOW variants depending on whether the player had the quest active/rewardable/
        // startable. QuestEngine/QuestState/PacketSendUtility aren't exposed to scripts yet.
    }

    // note: Java's onDialogSelect (no C# equivalent hook) ran QuestEngine.onDialog, then — for the three
    // "treasure room" dialog ids — consumed an Emperor/Empress/Prince key item and teleported the player
    // to one of three fixed points per key (TeleportService2), or sent a system message when the player
    // lacked the key. Inventory item consumption and TeleportService2 aren't exposed to scripts yet.
}

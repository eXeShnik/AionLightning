// QuestStartItemNpcAi2 — Java ai/quests/QuestStartItemNpcAi2.java. "Use item" NPC that opens the
// quest-start dialog for whichever quests the NPC can trigger.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("quest_start_use_item")]
public sealed class QuestStartItemNpcAi2 : ActionItemNpcAI2
{
    protected override void HandleUseItemFinish(Player player)
    {
        // note: Java looked up the NPC's onQuestStart quest list via QuestEngine, resolved a start
        // dialog through AI2Actions.selectDialog, and fell back to SM_DIALOG_WINDOW(SELECT_ACTION_1011)
        // when dialog selection failed. QuestEngine, AI2Actions and SM_DIALOG_WINDOW aren't reachable
        // from the script layer.
    }
}

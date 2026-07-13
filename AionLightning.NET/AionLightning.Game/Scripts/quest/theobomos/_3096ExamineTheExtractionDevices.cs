// Port of Java data/scripts/system/handlers/quest/theobomos/_3096ExamineTheExtractionDevices.java.
// Repeatable (max_repeat_count=255) collection quest: talk to Metatron (798225) to start; examine
// the four extraction devices (700423-700426) to gather one of each item (182208067-70, granted
// via the quest_drop mechanism, not this handler); turn in at Metatron for exp/gold, completing
// directly (no REWARD-status select-reward step). Java's manual reward math is ported through
// QuestRewardService.GrantAndCompleteAsync (FinishQuestAsync) instead of duplicating it inline,
// matching the precedent in _3055FugitiveScopind.cs/_3103KyprosDesire.cs; unlike Java, this aborts
// if the four collect items aren't actually present rather than completing anyway (Java's
// QUEST_SELECT case falls through into the unconditional completion body when the item check
// fails - not reproduced, since that fallthrough looks like an authoring bug rather than intended
// design).
// Skip vs Java: the device npcs' own case-to-case switch fallthrough (checking a different
// device's item id when the current one's guard doesn't return) is omitted - every branch is a
// pure no-op guard (no state changes), so this has no observable effect.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Theobomos;

public sealed class _3096ExamineTheExtractionDevices : QuestHandlerBase
{
    private const int QuestIdConst = 3096;
    private const int MetatronNpc  = 798225;
    private const int Device1Npc   = 700423;
    private const int Device2Npc   = 700424;
    private const int Device3Npc   = 700425;
    private const int Device4Npc   = 700426;
    private const int Item1 = 182208067;
    private const int Item2 = 182208068;
    private const int Item3 = 182208069;
    private const int Item4 = 182208070;

    public _3096ExamineTheExtractionDevices(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MetatronNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MetatronNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Device1Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Device2Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Device3Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Device4Npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status is QuestStatus.NONE or QuestStatus.COMPLETE)
        {
            if (targetId != MetatronNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == MetatronNpc)
        {
            bool hasAll = (player.Inventory.FindByItemId(Item1)?.Count ?? 0) >= 1
                       && (player.Inventory.FindByItemId(Item2)?.Count ?? 0) >= 1
                       && (player.Inventory.FindByItemId(Item3)?.Count ?? 0) >= 1
                       && (player.Inventory.FindByItemId(Item4)?.Count ?? 0) >= 1;

            if (dialog == DialogAction.QUEST_SELECT && hasAll)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            if (dialog == DialogAction.SELECTED_QUEST_NOREWARD)
            {
                await FinishQuestAsync(conn, player, 0, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (dialog == DialogAction.USE_OBJECT)
        {
            int requiredItem = targetId switch
            {
                Device1Npc => Item1,
                Device2Npc => Item2,
                Device3Npc => Item3,
                Device4Npc => Item4,
                _ => 0,
            };
            if (requiredItem != 0 && (player.Inventory.FindByItemId(requiredItem)?.Count ?? 0) < 1)
                return true;
        }
        return false;
    }
}

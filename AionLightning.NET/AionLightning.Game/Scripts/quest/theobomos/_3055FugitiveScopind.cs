// Port of Java data/scripts/system/handlers/quest/theobomos/_3055FugitiveScopind.java.
// Accept at 730146; turn in the tracking log (182208040, sourced from a mob drop declared in
// quest_data.xml) at 798195 to complete directly (exp/gold/reward_item payout, no REWARD-status
// select-reward step). Java's manual reward math (reads Rewards.get(0), grants exp/kinah/items,
// removes the collect item) is ported through the shared QuestRewardService.GrantAndCompleteAsync
// path (FinishQuestAsync) instead of duplicating it inline - equivalent since this quest has no
// kill objectives, matching the precedent in quest/theobomos/_3103KyprosDesire.cs.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Theobomos;

public sealed class _3055FugitiveScopind : QuestHandlerBase
{
    private const int QuestIdConst = 3055;
    private const int StartNpc     = 730146;
    private const int TurnInNpc    = 798195;
    private const int LogItemId    = 182208040;

    public _3055FugitiveScopind(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            long itemCount = player.Inventory.FindByItemId(LogItemId)?.Count ?? 0;
            if (dialog == DialogAction.QUEST_SELECT && itemCount >= 1)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECTED_QUEST_NOREWARD)
            {
                await FinishQuestAsync(conn, player, 0, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
        }
        return false;
    }
}

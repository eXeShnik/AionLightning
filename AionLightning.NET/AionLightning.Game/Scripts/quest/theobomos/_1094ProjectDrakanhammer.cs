// Port of Java data/scripts/system/handlers/quest/theobomos/_1094ProjectDrakanhammer.java.
// Zone-mission quest chained off 1093: report to Nestor (203834, var 0->1) then Atropos (798155,
// var 1->2, plays movie 367); interact with the Research Diary (700411, var 2->3, grants a diary
// item); interact with the Assistant's Journal (730153, var 3, consumes the diary, flips straight
// to REWARD); turn in at Nestor.
// Skip vs Java: the outer dispatch is a `switch(var)` with no break between cases (each case body
// only acts when its own npc/var combination matches, so the fallthrough is a no-op in the two
// dialog-only cases whose actions are gated by QuestHandlerBase helpers that already re-check the
// current var) - written here as independent per-npc blocks for clarity, with explicit var guards
// added on the two item-object cases so a stray fallthrough can't skip steps.
// Skip vs Java: QuestService.collectItemCheck(env, true) at the Assistant's Journal discards its
// boolean result in the original source (the quest completes unconditionally regardless of
// whether the mission's four collect_items were actually gathered) - omitted rather than ported,
// since replicating "check but ignore the result" would require inventing item-consumption code
// beyond the frozen helper set for no observable difference.
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

public sealed class _1094ProjectDrakanhammer : QuestHandlerBase
{
    private const int QuestIdConst        = 1094;
    private const int NestorNpc           = 203834;
    private const int AtroposNpc          = 798155;
    private const int ResearchDiaryObj    = 700411;
    private const int AssistantsJournalObj = 730153;
    private const int ResearchDiaryItem   = 182208017;

    private readonly IItemDao _itemDao;

    public _1094ProjectDrakanhammer(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(NestorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AtroposNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ResearchDiaryObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AssistantsJournalObj).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 1093, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestIds: [1091, 1093], isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != NestorNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == NestorNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (targetId == AtroposNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SELECT_ACTION_1353)
            {
                await PlayQuestMovieAsync(conn, player, 367, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }

        if (targetId == ResearchDiaryObj && var == 2)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, ResearchDiaryItem, 1, ct)) return false;
                await CloseDialogWindowAsync(conn, targetObjId, ct);
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                return true;
            }
            return false;
        }

        if (targetId == AssistantsJournalObj && var == 3)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, ResearchDiaryItem, 1, ct);
                entry.SetVar(0, 4);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
        }

        return false;
    }
}

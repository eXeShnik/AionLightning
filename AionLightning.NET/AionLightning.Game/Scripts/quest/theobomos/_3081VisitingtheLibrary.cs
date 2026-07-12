// Port of Java data/scripts/system/handlers/quest/theobomos/_3081VisitingtheLibrary.java.
// Talk to Atropos (798155) to start; report to the middle npc (203830) to advance; the quest can
// then be turned in either via Atropos (shows the generic reward-confirm page, then completes on
// the next visit) or directly via the librarian (798116, which sets var 0 to 3 and completes
// immediately in the same click) — both paths are genuinely reachable (unlike the other Theobomos
// "3-hop relay" quests, 798116 is a distinct npc from the start npc, not a duplicate/dead branch),
// so both are ported as written.
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

public sealed class _3081VisitingtheLibrary : QuestHandlerBase
{
    private const int QuestIdConst  = 3081;
    private const int AtroposNpc    = 798155;
    private const int MiddleNpc     = 203830;
    private const int LibrarianNpc  = 798116;

    public _3081VisitingtheLibrary(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AtroposNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AtroposNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MiddleNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LibrarianNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == AtroposNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (targetId == MiddleNpc && entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, entry.GetVar(0) + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 10, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (targetId == LibrarianNpc && entry is not null)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD
                && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
            {
                entry.SetVar(0, 3);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}

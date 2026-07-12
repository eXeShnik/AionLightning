// Port of Java data/scripts/system/handlers/quest/theobomos/_3076BolsteringtheAethericField.java.
// Talk to Atropos (798155) to start; report to two middle npcs (278503 then 278556) in
// sequence; return to Atropos to finish. Java's trailing "else if (targetId == 798155)" branch
// duplicates the first — unreachable dead code (the first branch already claims that npc id and
// always returns), so it's omitted here.
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

public sealed class _3076BolsteringtheAethericField : QuestHandlerBase
{
    private const int QuestIdConst = 3076;
    private const int AtroposNpc   = 798155;
    private const int MiddleNpc1   = 278503;
    private const int MiddleNpc2   = 278556;

    public _3076BolsteringtheAethericField(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AtroposNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(AtroposNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MiddleNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MiddleNpc2).OnTalk.Add(QuestId);
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

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            if (targetId == MiddleNpc1 && entry.GetVar(0) == 0)
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
            if (targetId == MiddleNpc2 && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        return false;
    }
}

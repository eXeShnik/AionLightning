// Port of Java data/scripts/system/handlers/quest/heiron/_1620StartSpreadingTheNews.java.
// Talk to the start NPC (204519); advance at 790000 (var 0->1, generic page 10), then at 730001
// (var 1->2, page 1693); turn in at 203125 (var ->3, REWARD).
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

namespace Quest.Heiron;

public sealed class _1620StartSpreadingTheNews : QuestHandlerBase
{
    private const int QuestIdConst = 1620;
    private const int StartNpc     = 204519;
    private const int MidNpcOne    = 790000;
    private const int MidNpcTwo    = 730001;
    private const int EndNpc       = 203125;

    public _1620StartSpreadingTheNews(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpcOne).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpcTwo).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (targetId == MidNpcOne)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (targetId == MidNpcTwo)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, entry.GetVar(0) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 1693), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (targetId == EndNpc)
        {
            if (entry is not null)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD && entry.Status is not QuestStatus.COMPLETE and not QuestStatus.NONE)
                {
                    entry.SetVar(0, 3);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}

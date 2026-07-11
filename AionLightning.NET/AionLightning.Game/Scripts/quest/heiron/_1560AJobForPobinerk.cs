// Port of Java data/scripts/system/handlers/quest/heiron/_1560AJobForPobinerk.java.
// Talk to the start NPC (798103); advance at 798026 (var 0->1, generic page 10), then at 798357
// (var 1->2, generic page 10); turn in at the start NPC (var ->3, REWARD).
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

public sealed class _1560AJobForPobinerk : QuestHandlerBase
{
    private const int QuestIdConst = 1560;
    private const int StartNpc     = 798103;
    private const int MidNpcOne    = 798026;
    private const int MidNpcTwo    = 798357;

    public _1560AJobForPobinerk(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpcOne).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MidNpcTwo).OnTalk.Add(QuestId);
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
            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.SetVar(0, 3);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
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
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}

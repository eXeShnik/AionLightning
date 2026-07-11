// Port of Java data/scripts/system/handlers/quest/heiron/_1574AFeatForAVillage.java.
// Talk to the start/end NPC (730025) to start; advance through three villagers 204560 (var 0->1),
// 204561 (var 1->2), 204562 (var 2->3, generic page 10 each); turn in back at 730025 (REWARD).
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

public sealed class _1574AFeatForAVillage : QuestHandlerBase
{
    private const int QuestIdConst  = 1574;
    private const int StartEndNpc   = 730025;
    private const int VillagerOne   = 204560;
    private const int VillagerTwo   = 204561;
    private const int VillagerThree = 204562;

    public _1574AFeatForAVillage(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartEndNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartEndNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VillagerOne).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VillagerTwo).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VillagerThree).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartEndNpc)
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
        else if (targetId == VillagerOne)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
                return await AdvanceAsync(entry, conn, targetObjId, dialog, env, ct);
        }
        else if (targetId == VillagerTwo)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 1)
                return await AdvanceAsync(entry, conn, targetObjId, dialog, env, ct);
        }
        else if (targetId == VillagerThree)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 2)
                return await AdvanceAsync(entry, conn, targetObjId, dialog, env, ct);
        }
        return false;
    }

    private async ValueTask<bool> AdvanceAsync(QuestEntry entry, GsClientConnection conn, int targetObjId, DialogAction dialog, QuestEnv env, CancellationToken ct)
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

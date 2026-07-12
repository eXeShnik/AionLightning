// Port of Java data/scripts/system/handlers/quest/sanctum/_3210RescueHaorunerk.java (synchro2).
// Talk to 798318 to start; 798332 (var 0 (slot 0) ->1) then two independent mob kills tracked in
// separate var slots (215056 -> slot 1, 215080 -> slot 2); turn in at 798331 once both are 1.
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

namespace Quest.Sanctum;

public sealed class _3210RescueHaorunerk : QuestHandlerBase
{
    private const int QuestIdConst = 3210;
    private const int StartNpc     = 798318;
    private const int FirstMobNpc  = 798332;
    private const int TurnInNpc    = 798331;
    private const int FirstKillMob  = 215056;
    private const int SecondKillMob = 215080;

    public _3210RescueHaorunerk(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstMobNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FirstKillMob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(SecondKillMob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry  = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.ASK_QUEST_ACCEPT) return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                if (dialog == DialogAction.QUEST_REFUSE_1) return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1) return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == FirstMobNpc && entry.GetVar(0) == 0)
        {
            if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SELECT_ACTION_1012) return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
        }

        if (targetId == TurnInNpc)
        {
            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD && entry.GetVar(1) == 1 && entry.GetVar(2) == 1)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == FirstKillMob && entry.GetVar(1) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: false, ct);
            return true;
        }
        if (env.TargetId == SecondKillMob && entry.GetVar(2) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 2, 1, toReward: false, ct);
            return true;
        }
        return false;
    }
}

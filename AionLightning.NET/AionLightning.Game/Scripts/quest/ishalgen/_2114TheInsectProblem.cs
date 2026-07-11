// Port of Java data/scripts/system/handlers/quest/ishalgen/_2114TheInsectProblem.java.
// Talk to Nunhe (203533), choose one of two extermination paths: SETPRO1 → var 1 (kill 210734
// x10, var 1->10) or SETPRO2 → var 11 (kill 210380/210381 x10, var 11->20). Each path flips to
// REWARD on the 10th kill; finish with reward index var/10 - 1.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Ishalgen;

public sealed class _2114TheInsectProblem : QuestHandlerBase
{
    private const int QuestIdConst = 2114;
    private const int NunheNpc     = 203533;
    private const int MobA         = 210734;
    private const int MobB1        = 210380;
    private const int MobB2        = 210381;

    public _2114TheInsectProblem(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NunheNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NunheNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobA).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MobB1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MobB2).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (env.TargetId != NunheNpc) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SETPRO1:
                    if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                        return await SetPathAsync(conn, player, targetObjId, 1, ct);
                    return false;
                case DialogAction.SETPRO2:
                    if (await StartMissionAsync(conn, player, QuestStatus.START, ct))
                        return await SetPathAsync(conn, player, targetObjId, 11, ct);
                    return false;
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int var = entry.GetVar(0);
            switch (dialog)
            {
                case DialogAction.USE_OBJECT:
                    if (var == 10) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    if (var == 20) return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                    return false;
                case DialogAction.SELECTED_QUEST_NOREWARD:
                    if (await FinishQuestAsync(conn, player, var / 10 - 1, ct))
                    {
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }
        return false;
    }

    private async ValueTask<bool> SetPathAsync(GsClientConnection conn, Player player, int targetObjId, int startVar, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;
        entry.SetVar(0, startVar);
        await UpdateQuestStatusAsync(conn, entry, ct);
        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);
        int targetId = env.TargetId;

        if (targetId == MobA)
        {
            if (var is >= 1 and < 10) { entry.SetVar(0, var + 1); await UpdateQuestStatusAsync(conn, entry, ct); return true; }
            if (var == 10) { entry.Status = QuestStatus.REWARD; await UpdateQuestStatusAsync(conn, entry, ct); return true; }
        }
        else if (targetId == MobB1 || targetId == MobB2)
        {
            if (var is >= 11 and < 20) { entry.SetVar(0, var + 1); await UpdateQuestStatusAsync(conn, entry, ct); return true; }
            if (var == 20) { entry.Status = QuestStatus.REWARD; await UpdateQuestStatusAsync(conn, entry, ct); return true; }
        }
        return false;
    }
}

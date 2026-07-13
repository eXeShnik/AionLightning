// Port of Java data/scripts/system/handlers/quest/rider_quests/_14022TheTestOfTheHeart.java (pralinka).
// Zone-mission sub-quest of 14020: talk to StartNpc (203900, var0 0->1), talk to ConfidantNpc
// (203996, var0 1->2), kill 210808/210799 while var0 is 2..6 (increments each kill), a further kill
// at var0==7 flips straight to REWARD; ConfidantNpc's SETPRO3 dialog also flips to REWARD directly
// (a second, dialog-driven path to the same transition); turn in at ConfidantNpc.
// Java bug: onDialogEvent's switch on StartNpc (203900) had no break after QUEST_SELECT, so talking
// with var0!=0 fell through into SETPRO1's body; harmless there since defaultCloseDialog re-checks
// var0==0 itself. ConfidantNpc's (203996) switch had the same missing break: QUEST_SELECT with
// var0 neither 1 nor 7 fell through into SETPRO2's unconditional defaultCloseDialog(1,2) call. Both
// are re-gated here on their own actual dialog id so a QUEST_SELECT click can no longer trigger the
// next case's body.
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

namespace Quest.RiderQuests;

public sealed class _14022TheTestOfTheHeart : QuestHandlerBase
{
    private const int QuestIdConst = 14022;
    private const int StartNpc     = 203900;
    private const int ConfidantNpc = 203996;
    private const int MobA         = 210808;
    private const int MobB         = 210799;

    public _14022TheTestOfTheHeart(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ConfidantNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobA).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MobB).OnKill.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14020, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != MobA && env.TargetId != MobB) return false;

        int var = entry.GetVar(0);
        if (var >= 2 && var < 7)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 7)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (env.TargetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (env.TargetId == ConfidantNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    if (var == 7) await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId != ConfidantNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}

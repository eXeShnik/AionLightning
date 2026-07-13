// Port of Java data/scripts/system/handlers/quest/rider_quests/_24011FunnyFloatingFungus.java (pralinka).
// Zone-mission sub-quest of 24010: talk to 203558 (var0 0->1), talk to 203572 (movie 60, var0 1->2),
// kill 700092 5 times (var0 2->6, var1 index), then a final kill flips straight to REWARD; turn in
// back at 203558.
// Java bug: onDialogEvent's switches had no break after QUEST_SELECT, so a stray QUEST_SELECT at the
// wrong var fell through into SETPRO2's body and replayed movie 60 unconditionally (before
// defaultCloseDialog's own var==1 guard could reject it). Fixed here so the movie only plays on an
// actual SETPRO2 dialog.
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

public sealed class _24011FunnyFloatingFungus : QuestHandlerBase
{
    private const int QuestIdConst = 24011;
    private const int FungusTenderNpc = 203558;
    private const int OtherTenderNpc  = 203572;
    private const int FungusMob       = 700092;

    public _24011FunnyFloatingFungus(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(FungusMob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(FungusTenderNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(OtherTenderNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24010, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 0 && var < 6)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 6)
        {
            // Java changeQuestStep(env, 6, 6, true): status -> REWARD only, no dialog packet sent
            // here (unlike DefaultCloseDialogAsync, which would also open a selection dialog on the
            // just-killed NPC).
            await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: true, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == FungusTenderNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == OtherTenderNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    if (var != 1) return false;
                    await PlayQuestMovieAsync(conn, player, 60, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FungusTenderNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}

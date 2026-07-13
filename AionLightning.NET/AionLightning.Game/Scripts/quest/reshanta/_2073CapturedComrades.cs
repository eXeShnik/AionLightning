// Port of Java data/scripts/system/handlers/quest/reshanta/_2073CapturedComrades.java (MetaWind/kale, reworked vlog).
// Asmodian mirror of _1073. Level-up / zone-mission gated on 2701. Talk chain Jebal (278002, var0
// 0->1) -> Lakadi (278019, 1->2) -> Glati (278088, 2->3) -> Captured Asmodian Prisoner (253626):
// QUEST_SELECT shows 2034, SELECT_ACTION_2035 plays movie 294 + shows 2035, SETPRO4 escorts the
// prisoner to the Magic Ward coords (1295.06, 1499.04, 1571.19) advancing var0 3->4. Reaching the ward
// flips to REWARD + plays movie 290; losing the prisoner (or logging out at var0 4) reverts var0 4->3.
// Turn in at Lakadi (278019): USE_OBJECT shows 10002, else completes.
// Escort uses StartFollowToCoords + OnNpcReachTarget/OnNpcLostTarget; step advance chained via
// DefaultCloseDialogAsync (helpers do not advance the step).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Reshanta;

public sealed class _2073CapturedComrades : QuestHandlerBase
{
    private const int QuestIdConst = 2073;
    private const int Jebal    = 278002;
    private const int Lakadi   = 278019;
    private const int Glati    = 278088;
    private const int Prisoner = 253626;

    public _2073CapturedComrades(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { Jebal, Lakadi, Glati, Prisoner })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        RegisterOnLogOut(engine);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2701, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Jebal)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                // Java switch fallthrough: QUEST_SELECT(var!=0) falls into SETPRO1
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == Lakadi)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                // Java switch fallthrough: QUEST_SELECT(var!=1) falls into SETPRO2
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == Glati)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                // Java switch fallthrough: QUEST_SELECT(var!=2) falls into SETPRO3
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == Prisoner)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                // Java switch fallthrough: QUEST_SELECT(var!=3) falls into SELECT_ACTION_2035
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_ACTION_2035)
                {
                    await PlayQuestMovieAsync(conn, env.Player, 294, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2035, ct);
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    StartFollowToCoords(env, conn, (Npc)env.Target!, 1295.0565f, 1499.0419f, 1571.1864f);
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Lakadi)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 4)
        {
            entry.SetVar(0, 3);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => FollowEndAsync(env, conn, 4, 4, reward: true, movie: 290, ct);

    public override ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => FollowEndAsync(env, conn, 4, 3, reward: false, movie: 0, ct);

    // Java QuestHandler.defaultFollowEndEvent: advance var0 step->nextStep (or flip to REWARD) only
    // while the quest is at START and sitting on `step`, optionally playing a movie afterwards.
    private async ValueTask<bool> FollowEndAsync(QuestEnv env, GsClientConnection conn, int step, int nextStep, bool reward, int movie, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == step)
        {
            await ChangeQuestStepAsync(conn, entry, 0, nextStep, reward, ct);
            if (movie != 0) await PlayQuestMovieAsync(conn, env.Player, movie, ct);
            return true;
        }
        return false;
    }
}

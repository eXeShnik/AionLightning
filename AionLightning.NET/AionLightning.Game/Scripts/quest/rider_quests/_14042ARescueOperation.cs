// Port of Java data/scripts/system/handlers/quest/rider_quests/_14042ARescueOperation.java (pralinka).
// Level-up / zone-mission gated on 14040. Talk chain Sakmis (278502, var0 0->1) -> Nereus (278517,
// 1->2) -> Dactyl (278590, 2->3) -> Captured Elyos Prisoner (253623): QUEST_SELECT shows 2034,
// SELECT_ACTION_2035 plays movie 269 + shows 2035, SETPRO4 escorts the prisoner to the Magic Ward
// coords (1295.11, 1498.65, 1571.18) advancing var0 3->4. Reaching the ward flips to REWARD + plays
// movie 270; losing the prisoner reverts var0 4->3 (as does logging out at var0 4). Turn in at Nereus
// (278517): QUEST_SELECT shows 10002, SELECT_QUEST_REWARD opens reward page 5, a reward pick ends it.
// The escort uses the new StartFollowToCoords + OnNpcReachTarget/OnNpcLostTarget hooks; since those
// helpers do not advance the dialog step, the step change is chained via DefaultCloseDialogAsync
// (mirroring Java defaultStartFollowEvent -> defaultCloseDialog).
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

namespace Quest.RiderQuests;

public sealed class _14042ARescueOperation : QuestHandlerBase
{
    private const int QuestIdConst = 14042;
    private const int Sakmis   = 278502;
    private const int Nereus   = 278517;
    private const int Dactyl   = 278590;
    private const int Prisoner = 253623;

    public _14042ARescueOperation(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { Sakmis, Nereus, Dactyl, Prisoner })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        RegisterOnLogOut(engine);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14040, isZoneMission: true, ct);

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
            if (targetId == Sakmis)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                // Java switch fallthrough: QUEST_SELECT(var!=0) falls into SETPRO1
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == Nereus)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                // Java switch fallthrough: QUEST_SELECT(var!=1) falls into SETPRO2
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == Dactyl)
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
                    await PlayQuestMovieAsync(conn, env.Player, 269, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2035, ct);
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    StartFollowToCoords(env, conn, (Npc)env.Target!, 1295.1139f, 1498.6543f, 1571.1763f);
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Nereus)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
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
        => FollowEndAsync(env, conn, 4, 4, reward: true, movie: 270, ct);

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

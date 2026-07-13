// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41551AnInnocentPlan.java (Cheatkiller).
// Accept at 205966 (QUEST_ACCEPT_SIMPLE grants item 182212545). At 205966 talk (218499 in Java is the
// escort NPC): QUEST_SELECT shows 1011, SETPRO1 makes the NPC (218499) follow the player to Enos
// (205966), advancing var0 0->1. Reaching Enos flips to REWARD; losing the escort (or logging out at
// var0 1) reverts var0 1->0. Turn in at 205966: USE_OBJECT shows 10002, else removes 182212545 and
// completes.
// Escort uses StartFollowToNpc + OnNpcReachTarget/OnNpcLostTarget; step advance chained via
// DefaultCloseDialogAsync (helper does not advance the step).
// note: Java's onLogOut also removeEffect(10381) (disguise buff) - cosmetic, no effect API here, dropped.
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

namespace Quest.Tiamaranta;

public sealed class _41551AnInnocentPlan : QuestHandlerBase
{
    private const int QuestIdConst = 41551;
    private const int Enos       = 205966;
    private const int EscortNpc  = 218499;
    private const int QuestItem  = 182212545;

    private readonly IItemDao _itemDao;

    public _41551AnInnocentPlan(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        RegisterOnLogOut(engine);
        engine.RegisterQuestNpc(Enos).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Enos).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EscortNpc).OnTalk.Add(QuestId);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Enos)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                    await GiveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == EscortNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    StartFollowToNpc(env, conn, (Npc)env.Target!, Enos);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Enos)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, QuestItem, 1, ct);
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
        if (entry.GetVar(0) == 1)
        {
            // note: Java removeEffect(10381) here - no effect API, dropped (cosmetic).
            entry.SetVar(0, 0);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => FollowEndAsync(env, conn, 1, 2, reward: true, movie: 0, ct);

    public override ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => FollowEndAsync(env, conn, 1, 0, reward: false, movie: 0, ct);

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

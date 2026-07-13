// Port of Java data/scripts/system/handlers/quest/pandaemonium/_4212MissingSidrunerk.java (Cheatkiller).
// Accept at 204283. Talk chain 798065 (var0 0->1) -> 798058 (1->2) -> 798337 (2->3): at 798337 the NPC
// (798337) starts escorting the player to coords (505.69, 437.69, 885.18), advancing var0 2->3. Talking
// to 730208 despawns it (dropped - no despawn API). Reaching the coords flips to REWARD; losing the
// escort (or dying / logging out at var0 3) reverts var0 3->2. Turn in at 798058: USE_OBJECT shows
// 10002, else completes.
// Escort uses StartFollowToCoords + OnNpcReachTarget/OnNpcLostTarget; step advance chained via
// DefaultCloseDialogAsync (helper does not advance the step).
// note: Java sets a walker id + broadcasts START_EMOTE2 on the escort NPC - cosmetic movement/emotion,
// dropped (the follow subsystem drives the walk instead).
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

namespace Quest.Pandaemonium;

public sealed class _4212MissingSidrunerk : QuestHandlerBase
{
    private const int QuestIdConst = 4212;
    private const int StartNpc  = 204283;
    private const int Npc65     = 798065;
    private const int Npc58     = 798058;
    private const int EscortNpc = 798337;
    private const int DespawnNpc = 730208;

    public _4212MissingSidrunerk(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc65).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc58).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EscortNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DespawnNpc).OnTalk.Add(QuestId);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
        RegisterOnDie(engine);
        RegisterOnLogOut(engine);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Npc65)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == Npc58)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == EscortNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    // note: Java setWalkerId("4212") + START_EMOTE2 dropped - the follow subsystem drives the walk.
                    StartFollowToCoords(env, conn, (Npc)env.Target!, 505.69427f, 437.69382f, 885.1844f);
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                return false;
            }
            if (targetId == DespawnNpc)
            {
                // note: Java npc.getController().delete() despawn dropped - no despawn API.
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Npc58)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 3)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 3)
        {
            entry.SetVar(0, 2);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => FollowEndAsync(env, conn, 3, 4, reward: true, movie: 0, ct);

    public override ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => FollowEndAsync(env, conn, 3, 2, reward: false, movie: 0, ct);

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

// Port of Java data/scripts/system/handlers/quest/sarpan/_41170ReiScued.java (Cheatkiller).
// Accept at 205744. At 205761: QUEST_SELECT (var0==0) shows 1011, SETPRO1 makes the NPC (205761) follow
// the player to 205744, advancing var0 0->1. Reaching 205744 flips to REWARD; losing the escort (or
// logging out at var0 1) reverts var0 1->0. Turn in at 205744: QUEST_SELECT shows 10002, else completes.
// Escort uses StartFollowToNpc + OnNpcReachTarget/OnNpcLostTarget; step advance chained via
// DefaultCloseDialogAsync (helper does not advance the step).
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

namespace Quest.Sarpan;

public sealed class _41170ReiScued : QuestHandlerBase
{
    private const int QuestIdConst = 41170;
    private const int StartNpc  = 205744;
    private const int EscortNpc = 205761;

    public _41170ReiScued(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in new[] { StartNpc, EscortNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        RegisterOnLogOut(engine);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
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
            if (targetId == EscortNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    StartFollowToNpc(env, conn, (Npc)env.Target!, StartNpc);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
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
            entry.SetVar(0, 0);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => FollowEndAsync(env, conn, 1, 1, reward: true, movie: 0, ct);

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

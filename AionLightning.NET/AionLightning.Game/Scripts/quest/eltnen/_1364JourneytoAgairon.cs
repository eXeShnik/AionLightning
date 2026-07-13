// Port of Java data/scripts/system/handlers/quest/eltnen/_1364JourneytoAgairon.java (Rhys2002, reworked vlog).
// Start at Ernia (203944); escort Teos (203945) to Dellome (790007) in Agairon Village (var0 0->1,
// on reach var0 1 -> REWARD + movie 12, lost/logout rolls 1->0); talk to Dellome to finish.
// Uses the follow/escort subsystem (StartFollowToNpc + reach/lost hooks).
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

namespace Quest.Eltnen;

public sealed class _1364JourneytoAgairon : QuestHandlerBase
{
    private const int QuestIdConst = 1364;
    private const int ErniaNpc     = 203944;
    private const int TeosNpc      = 203945;
    private const int DellomeNpc   = 790007;

    public _1364JourneytoAgairon(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ErniaNpc).OnQuestStart.Add(QuestId);
        RegisterOnLogOut(engine);
        engine.RegisterQuestNpc(TeosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DellomeNpc).OnTalk.Add(QuestId);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == ErniaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == TeosNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                StartFollowToNpc(env, conn, (Npc)env.Target!, DellomeNpc);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == DellomeNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
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
            if (conn is not null) await UpdateQuestStatusAsync(conn, entry, ct);
            else await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
        await PlayQuestMovieAsync(conn, env.Player, 12, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
        return true;
    }
}

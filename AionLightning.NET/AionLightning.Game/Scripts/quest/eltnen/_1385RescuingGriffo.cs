// Port of Java data/scripts/system/handlers/quest/eltnen/_1385RescuingGriffo.java (MrPoke, remod Xitanium).
// Start at 204028; talk 204029 and escort Griffo to fixed coords (1923.47/2541.46/355.61) (var0 0->1,
// on reach var0 1->2 + movie 48, lost/logout rolls 1->0); turn in at 204028. 206041 is registered for
// talk but has no dialog case (kept faithful to Java). Uses the follow/escort subsystem (StartFollowToCoords).
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

public sealed class _1385RescuingGriffo : QuestHandlerBase
{
    private const int QuestIdConst = 1385;
    private const int StartNpc     = 204028;
    private const int UnusedNpc    = 206041;
    private const int GriffoNpc    = 204029;

    public _1385RescuingGriffo(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UnusedNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GriffoNpc).OnTalk.Add(QuestId);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
        RegisterOnLogOut(engine);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == StartNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (targetId == GriffoNpc)
        {
            if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    StartFollowToCoords(env, conn, (Npc)env.Target!, 1923.47f, 2541.46f, 355.61f);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
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
            if (conn is not null) await UpdateQuestStatusAsync(conn, entry, ct);
            else await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
        await PlayQuestMovieAsync(conn, env.Player, 48, ct);
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

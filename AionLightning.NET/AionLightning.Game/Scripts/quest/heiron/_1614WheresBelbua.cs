// Port of Java data/scripts/system/handlers/quest/heiron/_1614WheresBelbua.java (Rhys2002, reworked vlog).
// Start at Phuthollo (204519); find Belbua (204645) in the Mudthorn Experiment Lab and escort him to
// fixed coords (376/529/133) (var0 0->1, on reach var0 1 -> REWARD, lost/logout rolls 1->0); tell
// Phuthollo. Uses the follow/escort subsystem (StartFollowToCoords + reach/lost hooks).
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

namespace Quest.Heiron;

public sealed class _1614WheresBelbua : QuestHandlerBase
{
    private const int QuestIdConst = 1614;
    private const int PhutholloNpc = 204519;
    private const int BelbuaNpc    = 204645;

    public _1614WheresBelbua(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(PhutholloNpc).OnQuestStart.Add(QuestId);
        RegisterOnLogOut(engine);
        engine.RegisterQuestNpc(PhutholloNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BelbuaNpc).OnTalk.Add(QuestId);
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
            if (targetId == PhutholloNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == BelbuaNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                StartFollowToCoords(env, conn, (Npc)env.Target!, 376f, 529f, 133f);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == PhutholloNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
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

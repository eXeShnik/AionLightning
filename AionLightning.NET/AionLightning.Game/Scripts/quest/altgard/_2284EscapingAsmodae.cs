// Port of Java data/scripts/system/handlers/quest/altgard/_2284EscapingAsmodae.java (Cheatkiller).
// Start at 203645; talk 798040 (var0 0->1) which despawns 798040 and spawns 798041; talk 798041 and
// escort it to 798034 (var0 1->2, on reach var0 2 -> REWARD, lost/logout rolls 2->1); turn in at 798034.
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

namespace Quest.Altgard;

public sealed class _2284EscapingAsmodae : QuestHandlerBase
{
    private const int QuestIdConst = 2284;
    private const int StartNpc     = 203645;
    private const int Guard1Npc    = 798040;
    private const int Guard2Npc    = 798041;
    private const int TargetNpc    = 798034;

    public _2284EscapingAsmodae(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        RegisterOnLogOut(engine);
        engine.RegisterQuestNpc(Guard1Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Guard2Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TargetNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == Guard1Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    var pos = ((Npc)env.Target!).Position;
                    // note: Java npc.getController().onDelete() (despawn 798040) dropped — cosmetic
                    SpawnQuestNpc(pos.WorldId, pos.InstanceId, Guard2Npc, 2553.9f, 916.9f, 311.8f, 82);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == Guard2Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    StartFollowToNpc(env, conn, (Npc)env.Target!, TargetNpc);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TargetNpc)
        {
            if (dialog == DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 2)
        {
            entry.SetVar(0, 1);
            if (conn is not null) await UpdateQuestStatusAsync(conn, entry, ct);
            else await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }
}

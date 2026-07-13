// Port of Java data/scripts/system/handlers/quest/heiron/_1693AreYouMyFather.java (Cheatkiller).
// Start at 798386; talk 204514 (var0 0->1) which teleports the player into world 110010000; on entering
// that world 798388 is spawned at the player's spot; talk 798388 and escort it to 203893 (var0 1->2, on
// reach var0 2 -> REWARD, lost/logout rolls 2->0); talk 203893 to finish. Uses the follow/escort subsystem.
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

public sealed class _1693AreYouMyFather : QuestHandlerBase
{
    private const int QuestIdConst = 1693;
    private const int StartNpc     = 798386;
    private const int GuideNpc     = 204514;
    private const int FollowerNpc  = 798388;
    private const int FatherNpc    = 203893;
    private const int SpawnWorld   = 110010000;

    private static readonly int[] _npcs = [StartNpc, GuideNpc, FollowerNpc, FatherNpc];

    public _1693AreYouMyFather(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in _npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        RegisterOnLogOut(engine);
        engine.RegisterOnEnterWorld(QuestId);
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
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == GuideNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    // note: TeleportService2 relocation (110010000 1323/1511/567) dropped — plain relocation, state kept
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == FollowerNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    StartFollowToNpc(env, conn, (Npc)env.Target!, FatherNpc);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                return false;
            }

            if (targetId == FatherNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: true, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == FatherNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return ValueTask.FromResult(false);
        if (player.Position.WorldId == SpawnWorld)
        {
            var pos = player.Position;
            SpawnQuestNpc(pos.WorldId, pos.InstanceId, FollowerNpc, pos.X, pos.Y, pos.Z, 0);
            return ValueTask.FromResult(true);
        }
        return ValueTask.FromResult(false);
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 2)
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
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
        return true;
    }
}

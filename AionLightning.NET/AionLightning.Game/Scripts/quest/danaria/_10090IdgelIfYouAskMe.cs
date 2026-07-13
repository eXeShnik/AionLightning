// Port of Java data/scripts/system/handlers/quest/danaria/_10090IdgelIfYouAskMe.java (pralinka).
// Elyos solo-instance chain (world 301000000). Gheanne (800566, var 0->1) -> Platonos (800816,
// 1->2) -> silver cube (730734) enters the instance, spawns Kaza1 (800817), plays movie 851 (2->3)
// -> Kaza1 (3->4) -> kill rusted door (701545, 4->5) -> enter QUEST_10090_301000000 zone (5->6) ->
// Danuar shield (730735) at var 6 starts a 120s timer, spawns four adds (231067 x2 / 231069 x2)
// (6->7); the timer end spawns Kaza2 (800818) and advances 7->8 -> Kaza2 SET_SUCCEED flips to
// REWARD. Turn in at 800820. Unblocked by EnterInstanceAsync (Java InstanceService triad) +
// OnDieAsync/OnLogOutAsync (Java onDieEvent/onLogOutEvent).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Danaria;

public sealed class _10090IdgelIfYouAskMe : QuestHandlerBase
{
    private const int QuestIdConst = 10090;
    private const int InstanceWorldId = 301000000;
    private const string EnterZoneName = "QUEST_10090_301000000";

    public _10090IdgelIfYouAskMe(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        int[] npcIds = { 800566, 800816, 730734, 800817, 730735, 800818, 800820 };
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npcId in npcIds)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
        RegisterOnLogOut(engine);
        RegisterOnDie(engine);
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterQuestNpc(701545).OnKill.Add(QuestId);
        RegisterOnEnterZone(engine, EnterZoneName);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 10085, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct) // rusted door
        => DefaultOnKillEventAsync(env, conn, 701545, 4, 5, ct);

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 7) return false;

        SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800818, 564f, 426f, 95f, 119);
        await ChangeQuestStepAsync(conn, entry, 0, 8, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != EnterZoneName) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (entry.GetVar(0) != 5) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            // Java switch fallthrough (QUEST_SELECT -> SETPROx): harmless, DefaultCloseDialog self-guards var.
            if (targetId == 800566) // gheanne
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == 800816) // platonos
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            if (targetId == 730734) // silver cube
            {
                if (dialog == DialogAction.USE_OBJECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await EnterInstanceAsync(player, conn, InstanceWorldId, 209f, 506f, 154f, 0, ct);
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800817, 210f, 507f, 154f, 119);
                    await PlayQuestMovieAsync(conn, player, 851, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                    return true;
                }
                return false;
            }

            if (targetId == 800817) // kaza1
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }

            if (targetId == 730735) // danuar shield
            {
                if (dialog == DialogAction.USE_OBJECT && var == 6)
                {
                    StartQuestTimer(env, conn, 120); // 2 min
                    await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: false, ct);
                    int worldId = player.Position.WorldId;
                    int instanceId = player.Position.InstanceId;
                    float x = player.Position.X, y = player.Position.Y, z = player.Position.Z;
                    SpawnQuestNpc(worldId, instanceId, 231067, x + 7, y - 7, z, 100);
                    SpawnQuestNpc(worldId, instanceId, 231067, x - 7, y + 7, z, 100);
                    SpawnQuestNpc(worldId, instanceId, 231069, x + 9, y - 9, z, 100);
                    SpawnQuestNpc(worldId, instanceId, 231069, x - 9, y + 9, z, 100);
                    return true;
                }
                return false;
            }

            if (targetId == 800818) // kaza2
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 8)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    // note: Java teleports to 600060000 (1529,2934,223) here - non-entry relocation, dropped.
                    return await DefaultCloseDialogAsync(env, conn, 8, 9, reward: true, sameNpc: false, ct);
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == 800820)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);
        if (var > 2 && var < 8)
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
        int var = entry.GetVar(0);
        if (var > 2 && var < 8)
        {
            entry.SetVar(0, 2);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet (conn may be null).
            return true;
        }
        return false;
    }
}

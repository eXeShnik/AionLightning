// Port of Java data/scripts/system/handlers/quest/danaria/_20093FIAWOLFalsityIsAWayOfLife.java (pralinka).
// Asmodian mirror of _10093TheKazaIdentity (world 300900000). Lucullus (800835, var 0->1) enters
// the instance and spawns Runa (800839); kill Hyperion Officers/Specialists (230396/230397) x3
// (var 1->4) -> Runa (var 4, movie 856, spawns Lucullus 800846, 4->5) -> Lucullus 800846 (5->6) ->
// Danuar Idgel Cube (701558) at var 6 spawns Hiding Spirits (230402 x3) + Lucullus 800847 (6->7) ->
// kill 230402 x3 (7->10) -> Lucullus 800847 (10->11) -> Cube 701558 at var 11 (movie 858, spawns
// Skuldun 800848, 11->12) -> Skuldun (12->13) -> Vard 800529 (SETPRO10 flips to REWARD). Turn in at
// Marchutan (801328). Unblocked by EnterInstanceAsync + OnDieAsync/OnLogOutAsync.
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

public sealed class _20093FIAWOLFalsityIsAWayOfLife : QuestHandlerBase
{
    private const int QuestIdConst = 20093;
    private const int InstanceWorldId = 300900000;

    public _20093FIAWOLFalsityIsAWayOfLife(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { 800835, 800839, 800846, 701558, 800847, 800848, 800529, 801328 })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in new[] { 230396, 230397, 230402 })
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        RegisterOnLogOut(engine);
        RegisterOnDie(engine);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 20092, ct);

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);
        if (var > 0 && var < 13)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
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
        if (var > 0 && var < 13)
        {
            entry.SetVar(0, 0);
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet (conn may be null).
            return true;
        }
        return false;
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

            if (targetId == 800835) // lucullus
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await EnterInstanceAsync(player, conn, InstanceWorldId, 153f, 143f, 125f, 0, ct);
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800839, 146f, 144f, 125f, 119);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }

            if (targetId == 800839) // runa
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4)
                {
                    await PlayQuestMovieAsync(conn, player, 856, ct);
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800846, 138f, 157f, 121f, 105);
                    // note: Java despawns the talked-to NPC here - no despawn API, dropped (cosmetic).
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                }
                return false;
            }

            if (targetId == 800846) // lucullus
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    // note: Java despawns the talked-to NPC here - no despawn API, dropped (cosmetic).
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                }
                return false;
            }

            if (targetId == 701558) // Danuar Idgel Cube
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (var == 11) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO5)
                {
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 230402, 116f, 137f, 113f, 119);
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 230402, 117f, 145f, 113f, 119);
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 230402, 119f, 139f, 112f, 49);
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800847, 104f, 139f, 112f, 119);
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                }
                if (dialog == DialogAction.SETPRO8)
                {
                    await PlayQuestMovieAsync(conn, player, 858, ct);
                    SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 800848, 105f, 143f, 125f, 119);
                    // note: Java intra-instance TeleportService2 relocation (109,141,125) dropped - no equivalent.
                    return await DefaultCloseDialogAsync(env, conn, 11, 12, ct);
                }
                return false;
            }

            if (targetId == 800847) // lucullus
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 10)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7)
                {
                    // note: Java despawns the talked-to NPC here - no despawn API, dropped (cosmetic).
                    return await DefaultCloseDialogAsync(env, conn, 10, 11, ct);
                }
                return false;
            }

            if (targetId == 800848) // skuldun
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 12)
                    return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                if (dialog == DialogAction.SETPRO9)
                {
                    // note: Java teleports to 600050000 (416,424,289) + despawns NPC here - dropped (non-entry relocation / cosmetic).
                    return await DefaultCloseDialogAsync(env, conn, 12, 13, ct);
                }
                return false;
            }

            if (targetId == 800529) // vard
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 13)
                    return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
                if (dialog == DialogAction.SETPRO10)
                    return await DefaultCloseDialogAsync(env, conn, 13, 13, reward: true, sameNpc: false, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == 801328) // marchutan
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int targetId = env.TargetId;
        int questVar = entry.GetVar(0);

        if (targetId == 230396 || targetId == 230397)
        {
            if (questVar >= 1 && questVar < 4)
            {
                entry.SetVar(0, questVar + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            if (questVar == 4)
                await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct); // Java falls through to return false
        }
        else if (targetId == 230402)
        {
            if (questVar >= 7 && questVar < 10)
            {
                entry.SetVar(0, questVar + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            if (questVar == 10)
                await ChangeQuestStepAsync(conn, entry, 0, 11, toReward: false, ct); // Java falls through to return false
        }
        return false;
    }
}

// Port of Java data/scripts/system/handlers/quest/eltnen/_1031TheMandurisSecret.java (Xitanium, reworked vlog).
// Zone-mission quest: talk Aurelius (203902, var0 0->1); hunt 6 Manduri (210771/210758/210763/210764/
// 210759/210770, var0 1..6->7); report to Aurelius (var0 7->8); talk Archelaos (203936, var0 8->9);
// find Paper Glider (700179, var0 9->10); talk Melginie (204043, escort her to Celestine 204030,
// var0 10->11); on reach var0 11->12 (lost/logout rolls 11->10); talk Celestine (var0 12 -> REWARD);
// report to Aurelius. Uses the follow/escort subsystem (StartFollowToNpc + reach/lost hooks).
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

public sealed class _1031TheMandurisSecret : QuestHandlerBase
{
    private const int QuestIdConst = 1031;
    private const int AureliusNpc  = 203902;
    private const int ArchelaosNpc = 203936;
    private const int GliderObj    = 700179;
    private const int MelginieNpc  = 204043;
    private const int CelestineNpc = 204030;

    private static readonly int[] _mobs = [210771, 210758, 210763, 210764, 210759, 210770];
    private static readonly int[] _npcs = [AureliusNpc, ArchelaosNpc, GliderObj, MelginieNpc, CelestineNpc];

    public _1031TheMandurisSecret(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        RegisterOnLogOut(engine);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in _npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == AureliusNpc)
            {
                // Java switch fallthrough (QUEST_SELECT -> SELECT_ACTION_1012 -> SETPRO1): all guarded by var, translated as guarded ifs.
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 7) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                if (dialog == DialogAction.SELECT_ACTION_1012 && var == 0)
                {
                    await PlayQuestMovieAsync(conn, player, 176, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                }
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                return false;
            }

            if (targetId == ArchelaosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 8) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 8, 9, ct);
                return false;
            }

            if (targetId == GliderObj && var == 9)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 10, toReward: false, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 0, ct);
                }
                return false;
            }

            if (targetId == MelginieNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 10) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                {
                    StartFollowToNpc(env, conn, (Npc)env.Target!, CelestineNpc);
                    return await DefaultCloseDialogAsync(env, conn, 10, 11, ct);
                }
                return false;
            }

            if (targetId == CelestineNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 12) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7) return await DefaultCloseDialogAsync(env, conn, 12, 12, reward: true, sameNpc: false, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == AureliusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (System.Array.IndexOf(_mobs, env.TargetId) >= 0 && var >= 1 && var <= 6)
        {
            entry.SetVar(0, var + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 11)
        {
            entry.SetVar(0, 10);
            if (conn is not null) await UpdateQuestStatusAsync(conn, entry, ct);
            else await QuestDao.UpsertAsync(player.ObjectId, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 11) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 12, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 11) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 10, toReward: false, ct);
        return true;
    }
}

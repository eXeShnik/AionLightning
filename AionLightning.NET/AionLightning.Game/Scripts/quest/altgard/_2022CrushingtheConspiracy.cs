// Port of Java data/scripts/system/handlers/quest/altgard/_2022CrushingtheConspiracy.java
// (HGabor85, reworked vlog/apozema/Gigi). Zone-mission quest chained off _2200AltgardDuties: talk
// to Suthran (203557), use the Abyss Gate Guardian Stone (700140) to spawn Kuninasha (214103), kill
// it, then use the Abyss Gate (700141) to play movie 154 and flip to REWARD; turn in at Suthran.
// Skip vs Java: SETPRO1's TeleportService2.teleportTo (into the abyss zone) and onMovieEndEvent's
// return teleport have no TeleportService2 port here, so only the var/status transitions survive -
// same "no dead branch, quest stays completable" rationale as the _1002RequestoftheElim precedent
// (see migration_plan.md). onDieEvent (var 2 -> 1 revert on death) is omitted entirely: this port
// has no OnDie hook (see IQuestHandler).
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

namespace Quest.Altgard;

public sealed class _2022CrushingtheConspiracy : QuestHandlerBase
{
    private const int QuestIdConst      = 2022;
    private const int SuthranNpc        = 203557;
    private const int GuardianStoneObj  = 700140;
    private const int AbyssGateObj      = 700141;
    private const int KuninashaNpc      = 214103;
    private const int AbyssWorldId      = 320030000;

    public _2022CrushingtheConspiracy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(SuthranNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GuardianStoneObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AbyssGateObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KuninashaNpc).OnKill.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(154, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null) return false;
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SuthranNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    // Java bug/gap: TeleportService2.teleportTo(player, 220030000, 2462.5815f,
                    // 2550.077f, 316.12088f, 66) sends the player into the abyss zone here - no
                    // TeleportService2 exists in this port, so only the var/status transition survives.
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SELECT_ACTION_1013)
                {
                    await PlayQuestMovieAsync(conn, player, 66, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1013, ct);
                }
                return false;
            }

            if (targetId == GuardianStoneObj && var == 2)
            {
                if (dialog != DialogAction.USE_OBJECT) return false;
                // TODO (ported from Java): no check that Kuninasha isn't already spawned, so talking
                // again while var == 2 can spawn a duplicate.
                SpawnQuestNpc(AbyssWorldId, player.Position.InstanceId, KuninashaNpc, 258.96942f, 239.16132f, 217.90526f, 94);
                return await UseQuestObjectAsync(env, conn, 2, 3, false, false, ct);
            }

            if (targetId == AbyssGateObj && var == 4)
            {
                if (dialog != DialogAction.USE_OBJECT) return false;
                await ChangeQuestStepAsync(conn, entry, 4, 4, toReward: true, ct);
                await PlayQuestMovieAsync(conn, player, 154, ct);
                return true;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == SuthranNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;
        if (player.Position.WorldId != AbyssWorldId) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KuninashaNpc, 3, 4, ct);

    public override ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        // Java bug/gap: TeleportService2.teleportTo(player, 220030000, 1664.7611f, 1749.524f,
        // 260.10562f, 68) returns the player from the abyss zone - no TeleportService2 in this port,
        // so this is a no-op besides acknowledging the movie end (same rationale as SETPRO1 above).
        return ValueTask.FromResult(movieId == 154);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2200, isZoneMission: true, ct);
}

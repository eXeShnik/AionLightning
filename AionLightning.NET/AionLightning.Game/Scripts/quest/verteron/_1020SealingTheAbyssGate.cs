// Port of Java data/scripts/system/handlers/quest/verteron/_1020SealingTheAbyssGate.java (Atomics/vlog/apozema).
// Talk Spatalos (203098, var0 0->1) -> enter world 310030000 (var0 1->2 via OnEnterWorld) -> use the
// Abyss Gate Guardian Stone (700142) to spawn Kuninasha (210753) -> kill Kuninasha (drops the Seal
// 182200024 via item data) -> use the Abyss Gate (700143) while holding the Seal, which after a 3s
// cast (if still targeting the gate) flips to REWARD and plays movie 153 -> turn in at Spatalos.
// New capability used: none of the new instance hooks are needed here — the 310030000 instance is
// entered elsewhere; this handler only reacts via OnEnterWorld. RegisterOnDie is now available and is
// wired for the player-death rollback.
// Skips vs Java: (1) Spatalos SETPRO1's TeleportService2 relocation to 210030000 (2683/1068/199) and
// the movie-153 relocation back to 210030000 (1724/1493/121) are plain relocations, not instance
// entries — dropped with notes, state transitions kept. (2) onKillEvent uses Java's
// defaultOnKillEvent(210753, 2, 2) which is a no-op (startVar==endVar) — ported verbatim.
// Java switch fallthrough on Spatalos (QUEST_SELECT had no break before SETPRO1) is dispatched
// explicitly with per-dialog var guards (same precedent as brusthonin/_2094, rider_quests/_24095).
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

namespace Quest.Verteron;

public sealed class _1020SealingTheAbyssGate : QuestHandlerBase
{
    private const int QuestIdConst   = 1020;
    private const int SpatalosNpc    = 203098;
    private const int GuardianStone  = 700142;
    private const int AbyssGate      = 700143;
    private const int KuninashaNpc   = 210753;
    private const int SealItem       = 182200024;
    private const int InstanceWorld  = 310030000;

    private static readonly int[] _verteronQuests =
        [1130, 1011, 1012, 1013, 1014, 1015, 1021, 1016, 1018, 1017, 1019, 1022, 1023];

    private readonly IItemDao _itemDao;

    public _1020SealingTheAbyssGate(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        RegisterOnDie(engine);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(SpatalosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GuardianStone).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AbyssGate).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KuninashaNpc).OnKill.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(153, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _verteronQuests, isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, KuninashaNpc, startVar: 2, endVar: 2, ct); // Java no-op (start==end), ported verbatim

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SpatalosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                {
                    // note: TeleportService2 relocation to 210030000 (2683/1068/199) dropped — plain relocation, state kept
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SELECT_ACTION_1013)
                {
                    await PlayQuestMovieAsync(conn, player, 29, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1013, ct);
                }
                return false;
            }

            if (targetId == GuardianStone && var == 2 && dialog == DialogAction.USE_OBJECT)
            {
                SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, KuninashaNpc, 258.89917f, 237.20166f, 217.06035f, 0);
                return await UseQuestObjectAsync(env, conn, 2, 2, reward: false, dieObject: false, ct);
            }

            if (targetId == AbyssGate)
            {
                long seal = player.Inventory.FindByItemId(SealItem)?.Count ?? 0;
                if (seal == 1)
                    ScheduleDestroy(env, conn);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != SpatalosNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, SealItem, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 1 && player.Position.WorldId == InstanceWorld)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 2)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, SealItem, 1, ct);
            return true;
        }
        return false;
    }

    public override ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        // note: movie 153's TeleportService2 relocation back to 210030000 (1724/1493/121) dropped — plain relocation
        return ValueTask.FromResult(movieId == 153);
    }

    /// <summary>Java private destroy(): 3s after the gate is used, if the player is still targeting the
    /// gate, flip to REWARD and play movie 153 (Java changeQuestStep(2, 3, true) leaves var0 at 2).</summary>
    private void ScheduleDestroy(QuestEnv env, GsClientConnection conn)
    {
        var player    = env.Player;
        int gateObjId = env.Target?.ObjectId ?? 0;
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            try
            {
                if (player.Target?.ObjectId != gateObjId) return;
                var entry = player.Quests.Get(QuestId);
                if (entry is null || entry.GetVar(0) != 2) return;
                await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, CancellationToken.None);
                await PlayQuestMovieAsync(conn, player, 153, CancellationToken.None);
            }
            catch { }
        });
    }
}

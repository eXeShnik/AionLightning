// Port of Java data/scripts/system/handlers/quest/reshanta/_2071SpeakingBalaur.java (Rhys2002/vlog/apozema).
// Talk to Hisui (278003, var 0->1), Sinjah (278086, var 1->2), attack the guard 253610 near
// Sinjah's spawn point to advance (var 2->3), Grunn (278039, var 3->4), Kaoranerk (279027, var
// 4->5, then var 6 -> REWARD), Phosphor (204210, var 5->6, gives item 182205501). Zone-mission
// chain, level-up gated on 2701.
// Skip vs Java: npc.getController().onDie(player) (instant-kill the attacked guard via its AI
// controller) is a documented no-op, matching quest/brusthonin/_4077PorgusRoundup.cs - no NPC
// controller/kill infra in this port; the quest var still advances so progress isn't blocked. The
// two TeleportService2.teleportTo hops (Pandaemonium, Balaurea) also aren't ported - same
// precedent as quest/eltnen/_1430ATeleportationExperiment.cs.
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

namespace Quest.Reshanta;

public sealed class _2071SpeakingBalaur : QuestHandlerBase
{
    private const int QuestIdConst = 2071;
    private const int HisuiNpc     = 278003;
    private const int SinjahNpc    = 278086;
    private const int GrunnNpc     = 278039;
    private const int KaoranerkNpc = 279027;
    private const int PhosphorNpc  = 204210;
    private const int GuardNpc     = 253610;
    private const int TokenItem    = 182205501;
    private const float ProximityRange = 15f;

    private readonly IItemDao _itemDao;

    public _2071SpeakingBalaur(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(GuardNpc).OnAttack.Add(QuestId);
        foreach (int npc in new[] { HisuiNpc, SinjahNpc, GrunnNpc, KaoranerkNpc, PhosphorNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2701, isZoneMission: true, ct);

    public override async ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != GuardNpc || env.Target is null) return false;

        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;

        var spawn = DataManager.Spawns.GetFirstSpawnByNpcId(SinjahNpc);
        if (spawn is null) return false;

        var spot = spawn.Value.Spot;
        var spawnPosition = new Position(spot.X, spot.Y, spot.Z, 0, 0);
        if (spawnPosition.DistanceTo(env.Target.Position) > ProximityRange) return false;

        await PlayQuestMovieAsync(conn, env.Player, 289, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
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
        int var         = entry.GetVar(0);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != HisuiNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        switch (targetId)
        {
            case HisuiNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1 when var == 0:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case SinjahNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO2 when var == 1:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case GrunnNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 3:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SETPRO4 when var == 3:
                        return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                    default:
                        return false;
                }
            case KaoranerkNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 4:
                        return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    case DialogAction.QUEST_SELECT when var == 6:
                        return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    case DialogAction.SELECT_ACTION_3058:
                        await RemoveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
                        await PlayQuestMovieAsync(conn, player, 293, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 3058, ct);
                    case DialogAction.SETPRO5 when var == 4:
                        return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                    case DialogAction.SET_SUCCEED when var == 6:
                        return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                    default:
                        return false;
                }
            case PhosphorNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 5:
                        return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    case DialogAction.SETPRO6 when var == 5:
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6, reward: false, sameNpc: false,
                            giveItemId: TokenItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    default:
                        return false;
                }
            default:
                return false;
        }
    }
}

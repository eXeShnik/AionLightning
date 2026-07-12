// Port of Java data/scripts/system/handlers/quest/reshanta/_1075NewWings.java (Rhys2002/vlog/Antraxx).
// Talk to Tellus (278506, var 0->1, movie 272), Agemonerk (279023, var 1->2), Raithor (278643, var
// 2->3, spawns two 214102), kill a spawned 214102, report back to Raithor (var 3, reward direct to
// var 12), then turn in at Agemonerk. Zone-mission chain gated on BOTH 1701 and 1072, level-up gated
// the same way.
// Java bugs fixed: (1) Raithor's QUEST_SELECT branch for var==2 had an empty if-body, so it silently
// fell through into the spawn-monster SETPRO3 logic instead of showing dialog 1693 - added the
// missing `return sendQuestDialog(1693)`. (2) that same fallthrough (no break in Java's switch)
// meant a QUEST_SELECT click while var==3 and not yet killed would cascade into SETPRO4's
// unconditional reward-flip - ported with explicit `when` guards so each dialog id only fires its
// own branch. (3) the "did the player kill the spawned mob" flag was a plain instance field on the
// handler (shared across every player using this singleton handler, not per-player state) - moved
// to persisted quest var slot 1.
// Skip vs Java: the SM_EMOTION(START_FLYTELEPORT) broadcast on completion is a cosmetic
// nearby-player visual with no broadcast-to-nearby-players primitive available here - omitted.
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

namespace Quest.Reshanta;

public sealed class _1075NewWings : QuestHandlerBase
{
    private const int QuestIdConst  = 1075;
    private const int TellusNpc     = 278506;
    private const int AgemonerkNpc  = 279023;
    private const int RaithorNpc    = 278643;
    private const int SpawnedMob    = 214102;
    private const int ReshantaWorldId = 400010000;

    private static readonly int[] _precedingQuests = [1701, 1072];

    public _1075NewWings(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(SpawnedMob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(TellusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AgemonerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RaithorNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 1072, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _precedingQuests, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        if (env.TargetId != SpawnedMob) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;

        await ChangeQuestStepAsync(conn, entry, varIdx: 1, newValue: 1, toReward: false, ct);
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
        bool killed     = entry.GetVar(1) == 1;

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != AgemonerkNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        switch (targetId)
        {
            case TellusNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SELECT_ACTION_1013:
                        await PlayQuestMovieAsync(conn, player, 272, ct);
                        return false;
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    default:
                        return false;
                }
            case AgemonerkNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.SETPRO2 when var == 1:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    default:
                        return false;
                }
            case RaithorNpc:
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct); // Java bug fix: was an empty branch
                    case DialogAction.QUEST_SELECT when var == 3 && killed:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SETPRO3 when var == 2:
                        SpawnQuestNpc(ReshantaWorldId, player.Position.InstanceId, SpawnedMob, 2344.32f, 1789.96f, 2258.88f, 86);
                        SpawnQuestNpc(ReshantaWorldId, player.Position.InstanceId, SpawnedMob, 2344.51f, 1786.01f, 2258.88f, 52);
                        entry.SetVar(0, 3);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    case DialogAction.SETPRO4 when var == 3:
                        entry.SetVar(0, 12);
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            default:
                return false;
        }
    }
}

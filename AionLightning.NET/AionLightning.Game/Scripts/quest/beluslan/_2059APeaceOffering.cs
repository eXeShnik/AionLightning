// Port of Java data/scripts/system/handlers/quest/beluslan/_2059APeaceOffering.java (Hellboy aion4Free).
// Structurally identical to rider_quests _24053, but level-up gated on {2500, 2057} and zone-mission
// gated on 2057. Talk chain 204787 (var0 0->1, plays movie 252 on stray QUEST_SELECT/SELECT_ACTION_1012)
// -> 204795 (1->2) -> 204796 (2->3): on advancing to 3 it spawns the survivor (204806) at the player and
// escorts it to NPC 204813. Reaching there advances var0 3->4 and plays movie 253 (defaultFollowEndEvent
// 3,4,false,253); losing the survivor (or logging out at var0 3) rolls var0 3->2. Back at 204787:
// SET_SUCCEED (var0==4) flips to REWARD. Turn in at 204702.
// Unblocked by the follow/escort subsystem (Java QuestService.spawnQuestNpc + FOLLOW_ME +
// newFollowingToTargetCheckTask -> SpawnQuestNpcAndGet + StartFollowToNpc + onNpcReachTarget/onNpcLostTarget).
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

namespace Quest.Beluslan;

public sealed class _2059APeaceOffering : QuestHandlerBase
{
    private const int QuestIdConst = 2059;
    private const int Npc1     = 204787;
    private const int Npc2     = 204795;
    private const int Npc3     = 204796;
    private const int EndNpc   = 204702;
    private const int Survivor = 204806;
    private const int Target   = 204813;

    public _2059APeaceOffering(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        RegisterOnLogOut(engine);
        RegisterOnReachTarget(engine);
        RegisterOnLostTarget(engine);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in new[] { Npc1, Npc2, Npc3, EndNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, 2057, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, new[] { 2500, 2057 }, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == EndNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == Npc1)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                // Java switch fallthrough: QUEST_SELECT(var not 0/4) falls into SELECT_ACTION_1012
                await PlayQuestMovieAsync(conn, player, 252, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_1012)
            {
                await PlayQuestMovieAsync(conn, player, 252, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1)
            {
                if (var == 0)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                // Java switch fallthrough: SETPRO1(var!=0) falls into SET_SUCCEED
                if (var == 4)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (dialog == DialogAction.SET_SUCCEED)
            {
                if (var == 4)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
        }
        else if (targetId == Npc2)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return false; // Java switch fallthrough into SETPRO2 has no effect (guarded on same var)
            }
            if (dialog == DialogAction.SETPRO2)
            {
                if (var == 1)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
        }
        else if (targetId == Npc3)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return false; // Java switch fallthrough into SETPRO3 has no effect (guarded on same var)
            }
            if (dialog == DialogAction.SETPRO3)
            {
                if (var == 2)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    var survivor = SpawnQuestNpcAndGet(player.Position.WorldId, player.Position.InstanceId, Survivor,
                        player.Position.X, player.Position.Y, player.Position.Z, (byte)0);
                    if (survivor != null)
                        StartFollowToNpc(env, conn, survivor, Target);
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnLogOutAsync(QuestEnv env, GsClientConnection? conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) == 3)
        {
            entry.SetVar(0, 2); // Java changeQuestStep(3, 2, false)
            await QuestDao.UpsertAsync(player.ObjectId, entry, ct); // logout: persist only, no packet (conn may be null).
        }
        return false;
    }

    public override async ValueTask<bool> OnNpcReachTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultFollowEndEvent(env, 3, 4, false, 253) -> roll var0 3->4 then play movie 253
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
        await PlayQuestMovieAsync(conn, env.Player, 253, ct);
        return true;
    }

    public override async ValueTask<bool> OnNpcLostTargetAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultFollowEndEvent(env, 3, 2, false) -> roll var0 3->2 when var0 == 3
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }
}

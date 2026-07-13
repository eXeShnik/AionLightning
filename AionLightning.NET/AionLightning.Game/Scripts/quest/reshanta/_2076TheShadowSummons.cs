// Port of Java data/scripts/system/handlers/quest/reshanta/_2076TheShadowSummons.java (vlog).
// Elyos zone-mission chain. Phyper (798300, var 0->1) -> leaving BALTASAR_HILL_VILLAGE_220050000 at
// var 1 gives token 182205502 and advances (1->2) -> Khrudgelmir (204253, var 2->3, removes token at
// SETPRO3) -> Garm (204089, SETPRO4 enters Shadow Court instance 320120000, plays movie 423, 3->5) ->
// Underground Arena Exit (700369, USE_OBJECT var==5, 5->6) -> Khrudgelmir SET_SUCCEED (6 -> REWARD) ->
// turn in at Munin (203550). Dying or leaving the instance while var==5 reverts to var 3.
// Unblocked by RegisterOnLeaveZone/OnLeaveZoneAsync (Java onLeaveZoneEvent) + EnterInstanceAsync
// (Java InstanceService triad) + OnDieAsync (Java onDieEvent).
// Skip vs Java: the Underground Arena Exit's TeleportService2 hop to 120010000 (out of the instance)
// is a non-entry relocation, dropped with a note; the var 5->6 transition is kept.
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

public sealed class _2076TheShadowSummons : QuestHandlerBase
{
    private const int QuestIdConst = 2076;
    private const int PhyperNpc      = 798300;
    private const int KhrudgelmirNpc = 204253;
    private const int ArenaExitObj   = 700369;
    private const int GarmNpc        = 204089;
    private const int MuninNpc       = 203550;
    private const int TokenItem      = 182205502;
    private const int InstanceWorld  = 320120000;
    private const string LeaveZoneName = "BALTASAR_HILL_VILLAGE_220050000";

    private static readonly int[] _npcs = [PhyperNpc, KhrudgelmirNpc, ArenaExitObj, GarmNpc, MuninNpc];

    private readonly IItemDao _itemDao;

    public _2076TheShadowSummons(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in _npcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
        RegisterOnLeaveZone(engine, LeaveZoneName);
        RegisterOnDie(engine);
        engine.RegisterOnEnterWorld(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2701, isZoneMission: true, ct);

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
            switch (targetId)
            {
                case PhyperNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (dialog == DialogAction.SETPRO1)
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    break;

                case KhrudgelmirNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 2)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (dialog == DialogAction.QUEST_SELECT && var == 6)
                        return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    if (dialog == DialogAction.SETPRO3)
                    {
                        await RemoveQuestItemAsync(player, conn, _itemDao, TokenItem, 1, ct);
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    }
                    if (dialog == DialogAction.SET_SUCCEED)
                        return await DefaultCloseDialogAsync(env, conn, 6, 6, reward: true, sameNpc: false, ct);
                    break;

                case ArenaExitObj:
                    if (dialog == DialogAction.USE_OBJECT && var == 5)
                    {
                        // note: Java teleports out to 120010000 (981.6009, 1552.97, 210.46) here — non-entry relocation, dropped; var 5->6 kept.
                        await ChangeQuestStepAsync(conn, entry, 0, 6, toReward: false, ct);
                        return true;
                    }
                    break;

                case GarmNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 3)
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    if (dialog == DialogAction.SETPRO4)
                    {
                        await EnterInstanceAsync(player, conn, InstanceWorld, 591.47894f, 420.20865f, 202.97754f, 0, ct);
                        await PlayQuestMovieAsync(conn, player, 423, ct);
                        await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: false, ct);
                        return await CloseDialogWindowAsync(conn, targetObjId, ct);
                    }
                    break;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == MuninNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                // note: Java also cleans quest item 182205502 on turn-in; it is already removed at SETPRO3.
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnLeaveZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != LeaveZoneName) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 1) return false;

        await GiveQuestItemAsync(env.Player, conn, _itemDao, TokenItem, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (player.Position.WorldId == InstanceWorld || entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(0) == 5)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(0) == 5)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
            return true;
        }
        return false;
    }
}

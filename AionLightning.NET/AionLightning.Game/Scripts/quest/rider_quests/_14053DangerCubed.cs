// Port of Java data/scripts/system/handlers/quest/rider_quests/_14053DangerCubed.java (pralinka).
// Zone-mission sub-quest of 14050: talk to MainNpc (204501, var0 0->1), talk to WaypointNpc
// (204020, var0 1->2), back at MainNpc the quest_data.xml collect-item check flips straight to
// REWARD (var0 stays at 2) or MainNpc's SETPRO3 advances var0 2->3 as an alternate path;
// SELECT_ACTION_1694 plays a cutscene movie; turn in at MainNpc.
// Skip vs Java: TeleportService2.teleportTo (two call sites moving the player to Eltnen/Heiron
// coordinates mid-dialog) is omitted - no TeleportService equivalent in this port (same
// simplification as eltnen._1430ATeleportationExperiment); the var/status transitions are kept so
// the quest stays completable without the physical move.
// Java bug: onDialogEvent's switch on MainNpc (204501) had no break after QUEST_SELECT, so talking
// with var0 outside {0,2,3} fell through into SETPRO1's body (harmless - its own var0==0 guard
// re-checks) and then into CHECK_USER_HAS_QUEST_ITEM's body, which has no var guard of its own in
// Java and ran the full collect-item check unconditionally regardless of the actual dialog id.
// Fixed here by gating CheckQuestItemsAsync on the actual CHECK_USER_HAS_QUEST_ITEM dialog only
// (its own step parameter still enforces var0==2).
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

namespace Quest.RiderQuests;

public sealed class _14053DangerCubed : QuestHandlerBase
{
    private const int QuestIdConst = 14053;
    private const int MainNpc      = 204501;
    private const int WaypointNpc  = 204020;

    private readonly IItemDao _itemDao;

    public _14053DangerCubed(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(WaypointNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MainNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14050, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.REWARD)
            return targetId == MainNpc && await SendQuestEndDialogAsync(env, conn, ct);
        if (entry.Status != QuestStatus.START) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (targetId == MainNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1 && var == 0)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 2, reward: true, checkOkId: 5, checkFailId: 10001, ct);
            if (dialog == DialogAction.SELECT_ACTION_1694)
            {
                await PlayQuestMovieAsync(conn, player, 191, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO3 && var == 2)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (targetId == WaypointNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return var == 1 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2 && var == 1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            }
            return false;
        }
        return false;
    }
}

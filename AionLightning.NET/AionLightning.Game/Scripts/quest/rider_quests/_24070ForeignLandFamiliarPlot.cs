// Port of Java data/scripts/system/handlers/quest/rider_quests/_24070ForeignLandFamiliarPlot.java (pralinka).
// Zone-mission sub-quest of 24062: Aimah (205617, var0 0->1), hub npc 205585 (1->2, and later the
// collect-item check at var0==4 + turn-in), 205739 (2->3), use object 730468 (3->4, a relocation
// departure); turn in back at 205585.
// Skip vs Java: 730468's USE_OBJECT branch called TeleportService2.teleportTo(player, 600020000,
// ...) to send the player onward - no TeleportService exists in this port; the var transition
// (3->4) is kept so the quest stays completable without the relocation.
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

public sealed class _24070ForeignLandFamiliarPlot : QuestHandlerBase
{
    private const int QuestIdConst = 24070;
    private const int AimahNpc = 205617;
    private const int HubNpc   = 205585;
    private const int Npc205739 = 205739;
    private const int DepartureNpc = 730468;

    private readonly IItemDao _itemDao;

    public _24070ForeignLandFamiliarPlot(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(AimahNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HubNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc205739).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DepartureNpc).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24062, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int var0        = entry.GetVar(0);
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == AimahNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == HubNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 4, reward: true, checkOkId: 10000, checkFailId: 10001, ct);
                return false;
            }
            if (targetId == Npc205739)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3 && var0 == 2) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == DepartureNpc)
            {
                if (dialog == DialogAction.USE_OBJECT && var0 == 3)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct); // relocation skipped, see header
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == HubNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}

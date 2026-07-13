// Port of Java data/scripts/system/handlers/quest/rider_quests/_24051InvesetigateTheDisappearance.java (pralinka).
// Zone-mission sub-quest of 24050: Mani (204707, var0 0->1), Paeru (204749, var0 1->2, hands out an
// evidence item), Hammel (204800, var0 4->5, hands out a departure item), use the Port (700359) with
// the departure item to leave (var0 5, no state change), entering the MINE_PORT zone at var0==5
// schedules movie 236 after 10s, whose end flips the quest to REWARD; turn in at Mani.
// Skip vs Java: the Port (700359) USE_OBJECT branch called TeleportService2.teleportTo(...) to send
// the player onward - no TeleportService exists in this port, so the interaction is acknowledged
// (dialog closes) without actually relocating the player.
using System;
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

public sealed class _24051InvesetigateTheDisappearance : QuestHandlerBase
{
    private const int QuestIdConst = 24051;
    private const int ManiNpc  = 204707;
    private const int PaeruNpc = 204749;
    private const int HammelNpc = 204800;
    private const int PortNpc   = 700359;
    private const int EvidenceItem   = 182215375;
    private const int DepartureItem  = 182215376;
    private const int TicketItem     = 182215377;
    private const string MineZone = "MINE_PORT_220040000";

    private readonly IItemDao _itemDao;

    public _24051InvesetigateTheDisappearance(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(EvidenceItem, QuestId);
        engine.RegisterOnQuestMovieEnd(236, QuestId);
        RegisterOnEnterZone(engine, MineZone);
        foreach (int npc in new[] { ManiNpc, PaeruNpc, HammelNpc, PortNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 24050, isZoneMission: true, ct);

    public override ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != MineZone) return ValueTask.FromResult(false);
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 5)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                try { await PlayQuestMovieAsync(conn, player, 236, CancellationToken.None); } catch { }
            });
        }
        return ValueTask.FromResult(false);
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 236) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 5, toReward: true, ct);
        await RemoveQuestItemAsync(env.Player, conn, _itemDao, TicketItem, 1, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        int var0        = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == ManiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT && var0 == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == PaeruNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                        giveItemId: EvidenceItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            if (targetId == HammelNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var0 == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, DepartureItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                }
                return false;
            }
            if (targetId == PortNpc)
            {
                var ticket = player.Inventory.FindByItemId(TicketItem);
                if (dialog == DialogAction.USE_OBJECT && var0 == 5 && ticket is not null && ticket.Count >= 1)
                    return await CloseDialogWindowAsync(conn, targetObjId, ct); // teleport itself skipped, see header
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ManiNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, DepartureItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }
}

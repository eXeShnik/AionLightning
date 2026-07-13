// Port of Java data/scripts/system/handlers/quest/rider_quests/_14016AGateAgape.java (pralinka).
// Verteron zone-mission (level-up gated on 14011-14015): talk Munin (203098, var0 0->1), enter the
// instanced Verteron gate world 310030000 (portal-driven, not a dialog teleport) where onEnterWorld
// spawns the boss (233873); kill it to receive item 182215317; then USE the Gate Guardian Stone
// (700142) which collect-checks that item, flips to REWARD (var0 2->3) and plays movie 153; turn in
// at Munin. Dying or leaving 310030000 while carrying the item reverts var0 2->1 and removes it.
// Player-death revert uses the new OnDieAsync hook (Java onDieEvent). In-instance boss spawn uses
// SpawnQuestNpc (Java QuestService.addNewSpawn) with player.Position.InstanceId.
// Skip vs Java: Munin's SETPRO1 TeleportService2 relocation to Verteron (210030000, 2683/1068/199)
// is a non-entry relocation -> dropped with a note, the var0 0->1 transition is kept. The commented-
// out registerOnMovieEndQuest(153) in the Java source is left out (it was already disabled there).
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.RiderQuests;

public sealed class _14016AGateAgape : QuestHandlerBase
{
    private const int QuestIdConst = 14016;
    private const int MuninNpc     = 203098;
    private const int GateStoneObj = 700142;
    private const int BossNpc      = 233873;
    private const int BossItem     = 182215317;
    private const int InstanceWorld = 310030000;

    private static readonly int[] _verteronQuests = [14011, 14012, 14013, 14014, 14015];

    private readonly IItemDao _itemDao;

    public _14016AGateAgape(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        RegisterOnDie(engine);
        engine.RegisterQuestNpc(BossNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MuninNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GateStoneObj).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _verteronQuests, isZoneMission: true, ct);

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
            if (targetId == MuninNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    // note: TeleportService2 relocation to Verteron (210030000, 2683/1068/199) dropped — non-entry relocation; var0 0->1 kept
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == GateStoneObj)
            {
                if (dialog == DialogAction.USE_OBJECT && await CollectItemCheckAsync(player, conn, ct))
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
                    await PlayQuestMovieAsync(conn, player, 153, ct);
                    return true;
                }
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == MuninNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 2) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        await RemoveQuestItemAsync(player, conn, _itemDao, BossItem, 1, ct);
        return true;
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 2 && player.Position.WorldId != InstanceWorld)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, BossItem, 1, ct);
            return true;
        }
        if (var == 1 && player.Position.WorldId == InstanceWorld)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
            SpawnQuestNpc(InstanceWorld, player.Position.InstanceId, BossNpc, 258.89917f, 237.20166f, 217.06035f, 0);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 2 || env.TargetId != BossNpc) return false;

        if ((player.Inventory.FindByItemId(BossItem)?.Count ?? 0) < 1)
            return await GiveQuestItemAsync(player, conn, _itemDao, BossItem, 1, ct);
        return false;
    }

    /// <summary>Java QuestService.collectItemCheck(env, true) — verifies every quest_data.xml collect
    /// item is present, then consumes them, without any quest-state transition.</summary>
    private async ValueTask<bool> CollectItemCheckAsync(Player player, GsClientConnection conn, CancellationToken ct)
    {
        var collectItems = Template?.CollectItems?.Items;
        if (collectItems is not { Count: > 0 }) return true;

        foreach (var req in collectItems)
        {
            var item = player.Inventory.FindByItemId(req.ItemId);
            if (item is null || item.Count < req.Count) return false;
        }

        var partiallyConsumed = new List<Item>();
        foreach (var req in collectItems)
        {
            var item = player.Inventory.FindByItemId(req.ItemId)!;
            item.Count -= req.Count;
            if (item.Count <= 0)
            {
                player.Inventory.Remove(item.UniqueId);
                await _itemDao.DeleteAsync(item.UniqueId, ct);
                await conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
            }
            else
            {
                partiallyConsumed.Add(item);
            }
        }
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        if (partiallyConsumed.Count > 0)
            await conn.SendAsync(new SM_INVENTORY_ADD_ITEM(partiallyConsumed), ct);
        return true;
    }
}

// Port of Java data/scripts/system/handlers/quest/gelkmaros/_20023KumbandasWhereabouts.java (Nephis, reworked Gigi).
// Valetta (799226, var0 0->1) -> Munin (799292) collect-checks twice (1->2, 2->3) -> Valetta
// (3->4) -> Kumbanda's Remains object (204057, 4->5, gives 182207611) -> Munin SETPRO6 (5->6,
// removes 182207611) -> Aether Portal (730243) enters solo instance 300150000 (var0 stays, world
// change rolls 6->7 via OnEnterWorld) -> talk one of the Statues (799513/.../799516) three times
// (var1 0->3), the fourth flips var0 7->8 and plays movie 442 -> movie 442 spawns Kumbanda (216592)
// -> kill 216592 (8->9, spawns 799341) -> 799341 (9->10, gives 182207613) -> Aether Rift (700706)
// leaves the instance (10->11) -> Munin SET_SUCCEED flips to REWARD. Turn in at Valetta (799226).
// Unblocked by EnterInstanceAsync + OnDieAsync.
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

namespace Quest.Gelkmaros;

public sealed class _20023KumbandasWhereabouts : QuestHandlerBase
{
    private const int QuestIdConst    = 20023;
    private const int InstanceWorldId = 300150000;

    private static readonly int[] _npcIds = { 799226, 799292, 700810, 204057, 730243, 799513, 799341, 700706, 799515 };

    private readonly IItemDao _itemDao;

    public _20023KumbandasWhereabouts(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        RegisterOnDie(engine);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(216592).OnKill.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(442, QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        foreach (int npcId in _npcIds)
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.Step == 6 && player.Position.WorldId == InstanceWorldId)
        {
            entry.Step = 7; // Java: qs.setQuestVar(7)
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false; // Java returns false unconditionally
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.Step == 8)
        {
            entry.Step = 7; // Java: qs.setQuestVar(7)
            await UpdateQuestStatusAsync(conn, entry, ct);
            // note: Java sends SM_SYSTEM_MESSAGE QUEST_FAILED_$1 here - cosmetic notice, dropped.
        }
        return false; // Java returns false unconditionally
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, new[] { 20020, 2094 }, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == 216592 && entry.GetVar(0) == 8)
        {
            SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 799341, 561.8763f, 192.25128f, 135.88919f, 30);
            entry.SetVar(0, entry.GetVar(0) + 1);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != 442) return false;
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.Step != 8) return false;

        SpawnQuestNpc(InstanceWorldId, player.Position.InstanceId, 216592, 561.8763f, 192.25128f, 135.88919f, 30);
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
        int var1        = entry.GetVar(1);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == 799226)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status != QuestStatus.START) return false;

        if (targetId == 799226)
        {
            // Java switch fallthrough (QUEST_SELECT -> SETPROx): harmless, DefaultCloseDialog self-guards var.
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO4)
                return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            return false;
        }

        if (targetId == 700810)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return true; // loot
            return false;
        }

        if (targetId == 799292)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (var == 5) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (var == 11) return await SendQuestDialogAsync(conn, targetObjId, 1608, ct);
                return false;
            }
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
            {
                if (await CollectItemCheckAsync(player, conn, ct))
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct); // Java: SM_DIALOG_WINDOW page 10
                }
                return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            }
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            if (dialog == DialogAction.SETPRO6)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6, reward: false, sameNpc: false,
                    giveItemId: 0, giveItemCount: 0, removeItemId: 182207611, removeItemCount: 1, ct);
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, 11, 11, reward: true, sameNpc: false, ct);
            return false;
        }

        if (targetId == 204057)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 4)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SETPRO5)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 5, reward: false, sameNpc: false,
                    giveItemId: 182207611, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            return false;
        }

        if (targetId == 799341)
        {
            // Java switch fallthrough (QUEST_SELECT -> SETPRO10): harmless, DefaultCloseDialog self-guards var.
            if (dialog == DialogAction.QUEST_SELECT && var == 9)
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            if (dialog == DialogAction.SETPRO10)
                return await DefaultCloseDialogAsync(env, conn, _itemDao, 9, 10, reward: false, sameNpc: false,
                    giveItemId: 182207613, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
            return false;
        }

        if (targetId == 799513 || targetId == 799514 || targetId == 799515 || targetId == 799516)
        {
            // Java switch fallthrough (QUEST_SELECT -> SETPRO10).
            if (dialog == DialogAction.QUEST_SELECT && var == 7)
                return await SendQuestDialogAsync(conn, targetObjId, 4080, ct);
            if (dialog == DialogAction.SETPRO10)
            {
                if (var == 7 && var1 == 3)
                {
                    await PlayQuestMovieAsync(conn, player, 442, ct);
                    entry.Step = 8; // Java: qs.setQuestVar(8)
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: Java despawns the talked-to statue (npc.getController().onDie) here - no despawn API, dropped (cosmetic).
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct); // Java: SM_DIALOG_WINDOW page 10
                }
                if (var == 7 && var1 < 3)
                {
                    entry.SetVar(1, var1 + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    // note: Java despawns the talked-to statue (npc.getController().onDie) here - no despawn API, dropped (cosmetic).
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct); // Java: SM_DIALOG_WINDOW page 10
                }
                return false;
            }
            return false;
        }

        if (targetId == 730243)
        {
            if (dialog == DialogAction.USE_OBJECT && var >= 6)
                return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.QUEST_SELECT && var == 6)
                return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
            if (dialog == DialogAction.SETPRO7 && var > 5)
            {
                await EnterInstanceAsync(player, conn, InstanceWorldId, 561.8651f, 221.91483f, 134.53333f, 90, ct);
                return true;
            }
            return false;
        }

        if (targetId == 700706)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 10)
            {
                entry.Step = 11; // Java: qs.setQuestVar(11)
                await UpdateQuestStatusAsync(conn, entry, ct);
                // note: Java teleports to 220070000 (2274.86,1602.27,412.32) here - non-entry relocation, dropped.
            }
            return false; // Java falls through to return false
        }

        return false;
    }

    /// <summary>Java QuestService.collectItemCheck(env, true) — checks every quest_data.xml collect
    /// item is present, then consumes them all, without any quest-state transition.</summary>
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

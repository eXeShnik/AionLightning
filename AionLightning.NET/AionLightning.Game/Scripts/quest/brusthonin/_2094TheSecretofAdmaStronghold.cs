// Port of Java data/scripts/system/handlers/quest/brusthonin/_2094TheSecretofAdmaStronghold.java.
// Talk Surt (205150, var 0->1) -> talk 205192 (1->2 -> collect-check 2->3 -> 3->4, no ack sent on
// the last step, faithful to Java's missing return) -> kill 214700 (4->5) -> talk 205155 (5->6)
// -> use object 730164 (spawns 205191) -> use 205191 (flips to REWARD) -> turn in at 204057.
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

namespace Quest.Brusthonin;

public sealed class _2094TheSecretofAdmaStronghold : QuestHandlerBase
{
    private const int QuestIdConst  = 2094;
    private const int SurtNpc       = 205150;
    private const int GuideNpc      = 205192;
    private const int ScoutNpc      = 205155;
    private const int GateObjNpc    = 730164;
    private const int AdmaGuardNpc  = 205191;
    private const int TurnInNpc     = 204057;
    private const int RedLegionNpc  = 214700;

    private static readonly int[] _precedingZoneMissionQuests = [2092, 2093, 2054];
    private static readonly int[] _precedingLevelUpQuests     = [2091, 2092, 2093, 2054];

    private readonly IItemDao _itemDao;

    public _2094TheSecretofAdmaStronghold(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(RedLegionNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(SurtNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GuideNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ScoutNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GateObjNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AdmaGuardNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, _precedingZoneMissionQuests, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, _precedingLevelUpQuests, isZoneMission: true, ct);

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, RedLegionNpc, startVar: 4, endVar: 5, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TurnInNpc) return false;
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == SurtNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    return true;
                case DialogAction.SETPRO1 when var == 0:
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (targetId == GuideNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return true;
                case DialogAction.SETPRO2 when var == 1:
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM when var == 2:
                    if (await CollectItemCheckAsync(player, conn, ct))
                    {
                        entry.SetVar(0, var + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                    }
                    return await SendQuestDialogAsync(conn, targetObjId, 10008, ct);
                case DialogAction.SETPRO4 when var == 3:
                    // Java bug: the original SETPRO4 case falls through to `default: break;` with
                    // no dialog ack/return — the var still advances, but the client gets no
                    // response for this click. Preserved faithfully.
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return false;
                default:
                    return false;
            }
        }

        if (targetId == ScoutNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 5:
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                case DialogAction.SETPRO6 when var == 5:
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (targetId == GateObjNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 6)
            {
                var pos = env.Target?.Position ?? player.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, AdmaGuardNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                // Java also despawned this gate object (npc.getController().scheduleRespawn() +
                // onDelete()) — no NPC-controller despawn/respawn hook is exposed to quest
                // scripts yet (same documented gap as UseQuestObjectAsync's dieObject no-op), so
                // the replacement guard spawns alongside the still-present gate object.
                return true;
            }
            return false;
        }

        if (targetId == AdmaGuardNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && var == 6)
            {
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
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

// Port of Java data/scripts/system/handlers/quest/beluslan/_2056ThawingKurngalfberg.java.
// 204753 (var0->1->2), three witnesses hand out fire-source items (730036 gives 182204313,
// 279000 gives 182204314, 790016 gives 182204315) once each; using the matching item inside the
// DF3_ITEMUSEAREA_Q2056 zone advances var2->3->4->REWARD (movies 243/244/245), removing the used
// item each time. Skip vs Java: the 2s SM_ITEM_USAGE_ANIMATION cast delay on item use is omitted
// (cosmetic only).
using System.Linq;
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

public sealed class _2056ThawingKurngalfberg : QuestHandlerBase
{
    private const int QuestIdConst = 2056;
    private const int MainNpc = 204753;
    private const int WitnessGas = 790016;
    private const int WitnessFire = 730036;
    private const int WitnessAir = 279000;
    private const int FireItem = 182204313;
    private const int AirItem = 182204314;
    private const int GasItem = 182204315;
    private const string ItemUseZone = "DF3_ITEMUSEAREA_Q2056";

    private static readonly int[] _cleanupItems = [FireItem, AirItem, GasItem];

    private readonly IItemDao _itemDao;

    public _2056ThawingKurngalfberg(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(FireItem, QuestId);
        engine.RegisterQuestItem(AirItem, QuestId);
        engine.RegisterQuestItem(GasItem, QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MainNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WitnessGas).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WitnessFire).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WitnessAir).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2500, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == MainNpc)
            {
                if (dialog == DialogAction.USE_OBJECT) return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);

                foreach (int itemId in _cleanupItems)
                {
                    var leftover = player.Inventory.FindByItemId(itemId);
                    if (leftover is not null && leftover.Count > 0)
                        await RemoveQuestItemAsync(player, conn, _itemDao, itemId, leftover.Count, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }
        if (entry.Status != QuestStatus.START) return false;

        if (targetId == MainNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_1012)
            {
                await PlayQuestMovieAsync(conn, player, 242, ct);
                return false;
            }
            if (dialog == DialogAction.SELECT_ACTION_2376)
            {
                bool hasAllItems = _cleanupItems.All(itemId =>
                {
                    var item = player.Inventory.FindByItemId(itemId);
                    return item is not null && item.Count >= 1;
                });
                return await SendQuestDialogAsync(conn, targetObjId, hasAllItems ? 2376 : 2461, ct);
            }
            if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO5) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }
        if (targetId == WitnessGas)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SELECT_ACTION_2035)
            {
                if (var != 1) return false;
                var existing = player.Inventory.FindByItemId(GasItem);
                if (existing is null || existing.Count != 1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, GasItem, 1, ct))
                        return await SendQuestDialogAsync(conn, targetObjId, 2035, ct);
                    return true;
                }
                return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
            }
            return false;
        }
        if (targetId == WitnessFire)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SELECT_ACTION_1353)
            {
                if (var != 1) return false;
                var existing = player.Inventory.FindByItemId(FireItem);
                if (existing is null || existing.Count != 1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, FireItem, 1, ct))
                        return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                    return true;
                }
                return await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
            }
            return false;
        }
        if (targetId == WitnessAir)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            if (dialog == DialogAction.SELECT_ACTION_1694)
            {
                if (var != 1) return false;
                var existing = player.Inventory.FindByItemId(AirItem);
                if (existing is null || existing.Count != 1)
                {
                    if (await GiveQuestItemAsync(player, conn, _itemDao, AirItem, 1, ct))
                        return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                    return true;
                }
                return await SendQuestDialogAsync(conn, targetObjId, 1779, ct);
            }
            return false;
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        int var = entry.GetVar(0);
        bool mismatched = (itemId != FireItem && var == 2) || (itemId != AirItem && var == 3) || (itemId != GasItem && var == 4);
        if (mismatched) return false;

        if (var == 2)
        {
            await PlayQuestMovieAsync(conn, player, 243, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, itemId, 1, ct);
            entry.SetVar(0, 3);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (var == 3)
        {
            await PlayQuestMovieAsync(conn, player, 244, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, itemId, 1, ct);
            entry.SetVar(0, 4);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        if (var == 4 && entry.Status != QuestStatus.COMPLETE && entry.Status != QuestStatus.NONE)
        {
            await RemoveQuestItemAsync(player, conn, _itemDao, itemId, 1, ct);
            await PlayQuestMovieAsync(conn, player, 245, ct);
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }
}

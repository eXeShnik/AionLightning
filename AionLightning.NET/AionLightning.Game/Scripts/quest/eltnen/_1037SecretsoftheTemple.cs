// Port of Java data/scripts/system/handlers/quest/eltnen/_1037SecretsoftheTemple.java (Rhys2002).
// Zone-mission quest, part of the Kaidan Fortress chain (1300): talk to Castor (203965, var 0->1);
// Axelion (203967) hands out the temple key (182201027, var 2->3) and previews the wall-order
// collect check; use the five elemental walls (Flower/Lightning/Wave/Wind/Fire, var 3 through 7) in
// order; the Fire Wall flips to REWARD and consumes the key; turn in at Castor.
// Skip vs Java: giveQuestItem's onGetItemEvent side effect (Java's ItemService.addQuestItems fires
// the quest engine's item-get dispatch synchronously) has no equivalent - this port's
// GiveQuestItemAsync doesn't notify OnItemGetAsync, so the var 2->3 transition is applied directly
// alongside the give call here; OnItemGetAsync is still registered/implemented for parity in case
// the key is ever acquired through another source.
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

namespace Quest.Eltnen;

public sealed class _1037SecretsoftheTemple : QuestHandlerBase
{
    private const int QuestIdConst = 1037;
    private const int CastorNpc    = 203965;
    private const int AxelionNpc   = 203967;
    private const int FlowerWall   = 700151;
    private const int LightningWall = 700154;
    private const int WaveWall     = 700150;
    private const int WindWall     = 700153;
    private const int FireWall     = 700152;
    private const int TempleKeyItem = 182201027;

    private readonly IItemDao _itemDao;

    public _1037SecretsoftheTemple(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterItemGet(TempleKeyItem, QuestId);
        foreach (int npc in new[] { CastorNpc, AxelionNpc, FlowerWall, LightningWall, WaveWall, WindWall, FireWall })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == CastorNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == AxelionNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.SELECT_ACTION_1694:
                        return await SendQuestDialogAsync(conn, targetObjId, var == 2 && HasAllCollectItems(player) ? 1694 : 1779, ct);
                    case DialogAction.SETPRO2:
                        return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                    case DialogAction.SETPRO3:
                        await GiveQuestItemAsync(player, conn, _itemDao, TempleKeyItem, 1, ct);
                        if (entry.GetVar(0) == 2)
                            await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }

            if (dialog != DialogAction.USE_OBJECT) return false;

            if (targetId == FlowerWall) return await AdvanceWallAsync(conn, entry, 3, 4, ct);
            if (targetId == LightningWall) return await AdvanceWallAsync(conn, entry, 4, 5, ct);
            if (targetId == WaveWall) return await AdvanceWallAsync(conn, entry, 5, 6, ct);
            if (targetId == WindWall) return await AdvanceWallAsync(conn, entry, 6, 7, ct);
            if (targetId == FireWall)
            {
                if (entry.GetVar(0) != 7) return false;
                await ChangeQuestStepAsync(conn, entry, -1, 7, toReward: true, ct);
                await RemoveQuestItemAsync(player, conn, _itemDao, TempleKeyItem, 1, ct);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == CastorNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != TempleKeyItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }

    private async ValueTask<bool> AdvanceWallAsync(GsClientConnection conn, QuestEntry entry, int step, int nextStep, CancellationToken ct)
    {
        if (entry.GetVar(0) != step) return false;
        await ChangeQuestStepAsync(conn, entry, 0, nextStep, toReward: false, ct);
        return true;
    }

    private bool HasAllCollectItems(Player player)
    {
        var items = Template?.CollectItems?.Items;
        if (items is not { Count: > 0 }) return false;
        foreach (var req in items)
        {
            var item = player.Inventory.FindByItemId(req.ItemId);
            if (item is null || item.Count < req.Count) return false;
        }
        return true;
    }
}

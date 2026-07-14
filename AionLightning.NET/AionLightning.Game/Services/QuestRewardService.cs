using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Model.Templates.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.Services;

/// <summary>
/// Validates quest completion objectives and pays out quest rewards (exp, items, kinah, abyss
/// points, title). Shared by the legacy inline dialog handling (<see cref="Network.Aion.ClientPackets.CM_DIALOG_SELECT"/>)
/// and the data-driven quest engine handlers so the payout logic exists in exactly one place.
/// </summary>
public sealed class QuestRewardService
{
    private const int KinahItemId = 182400001;

    private readonly IDataManager             _dataManager;
    private readonly IQuestDao                _questDao;
    private readonly IItemDao                 _itemDao;
    private readonly IPlayerDao               _playerDao;
    private readonly ExperienceService        _expService;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly QuestEngineType          _questEngine;
    private readonly RateOptions              _rates;
    private readonly ILogger<QuestRewardService> _log;

    public QuestRewardService(IDataManager dataManager, IQuestDao questDao, IItemDao itemDao,
        IPlayerDao playerDao, ExperienceService expService, PlayerConnectionRegistry connRegistry,
        QuestEngineType questEngine, IOptions<RateOptions> rates, ILogger<QuestRewardService> log)
    {
        _dataManager  = dataManager;
        _questDao     = questDao;
        _itemDao      = itemDao;
        _playerDao    = playerDao;
        _expService   = expService;
        _connRegistry = connRegistry;
        _questEngine  = questEngine;
        _rates        = rates.Value;
        _log          = log;
    }

    /// <summary>
    /// Validates objectives (kills + collect items), consumes collect items, grants exp/items/
    /// gold/abyss points/title, marks the quest COMPLETE, and broadcasts the resulting packets.
    /// Returns false (no-op) if the quest isn't in a rewardable state or objectives aren't met yet.
    /// </summary>
    public async ValueTask<bool> GrantAndCompleteAsync(GsClientConnection conn, Player player,
        QuestEntry entry, QuestTemplate template, int rewardIndex, CancellationToken ct)
    {
        if (entry.Status == QuestStatus.COMPLETE) return false;

        // Multi-tier quests declare several sibling <rewards> blocks (e.g. relic_rewards' 4
        // reward_abyss_point tiers); rewardIndex then picks which whole block to use (Java:
        // template.getRewards().get(reward)). Single/no-tier quests keep using the existing
        // last-declared-block behavior via the Rewards fallback (no observable change for them).
        var rewards = template.RewardsList.Count > 1 && rewardIndex >= 0 && rewardIndex < template.RewardsList.Count
            ? template.RewardsList[rewardIndex]
            : template.Rewards;

        // When status is START, validate objectives here (REWARD state was already validated at transition)
        if (entry.Status == QuestStatus.START)
        {
            foreach (var kill in template.QuestKills)
                if (entry.GetVar(kill.Seq) < kill.Count) return false;

            if (template.CollectItems is { Items.Count: > 0 })
                foreach (var req in template.CollectItems.Items)
                {
                    var chk = player.Inventory.FindByItemId(req.ItemId);
                    if (chk is null || chk.Count < req.Count) return false;
                }

            if (template.InventoryItems is { Items.Count: > 0 })
                foreach (var req in template.InventoryItems.Items)
                {
                    var chk = player.Inventory.FindByItemId(req.ItemId);
                    if (chk is null || chk.Count < req.Count) return false;
                }
        }

        // Validate and consume collect_item requirements
        if (template.CollectItems is { Items.Count: > 0 })
        {
            foreach (var req in template.CollectItems.Items)
            {
                var item = player.Inventory.FindByItemId(req.ItemId);
                if (item is null || item.Count < req.Count) return false;
            }

            var partiallyConsumed = new List<Item>();
            foreach (var req in template.CollectItems.Items)
            {
                var item = player.Inventory.FindByItemId(req.ItemId);
                if (item is null) continue;
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
        }

        // Validate and consume inventory_items requirements (Java's InventoryItems presence-check +
        // decrease-by-count path — the "coin fountain"-style gate distinct from collect_items)
        if (template.InventoryItems is { Items.Count: > 0 })
        {
            foreach (var req in template.InventoryItems.Items)
            {
                var item = player.Inventory.FindByItemId(req.ItemId);
                if (item is null || item.Count < req.Count) return false;
            }

            var partiallyConsumedInv = new List<Item>();
            foreach (var req in template.InventoryItems.Items)
            {
                var item = player.Inventory.FindByItemId(req.ItemId);
                if (item is null) continue;
                item.Count -= req.Count;
                if (item.Count <= 0)
                {
                    player.Inventory.Remove(item.UniqueId);
                    await _itemDao.DeleteAsync(item.UniqueId, ct);
                    await conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
                }
                else
                {
                    partiallyConsumedInv.Add(item);
                }
            }
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            if (partiallyConsumedInv.Count > 0)
                await conn.SendAsync(new SM_INVENTORY_ADD_ITEM(partiallyConsumedInv), ct);
        }

        // Award experience (quest rate applied inside AddQuestExpAsync)
        long expReward = rewards?.Exp ?? 0;
        if (expReward > 0)
            await _expService.AddQuestExpAsync(player, expReward, conn, ct);

        // Award selected reward item
        var selectableItems = rewards?.SelectableItems;
        if (selectableItems is { Count: > 0 } && rewardIndex >= 0 && rewardIndex < selectableItems.Count)
        {
            var reward  = selectableItems[rewardIndex];
            int maxStk  = _dataManager.Items.GetTemplate(reward.ItemId)?.MaxStackCount ?? 1;
            if (player.Inventory.CanReceive(reward.ItemId, maxStk))
            {
                var existed = player.Inventory.FindByItemId(reward.ItemId);
                Item rewardItem;
                if (existed is not null)
                {
                    existed.Count += reward.Count;
                    rewardItem = existed;
                }
                else
                {
                    long uid = await _itemDao.NextUniqueIdAsync(ct);
                    rewardItem = new Item { UniqueId = uid, ItemId = reward.ItemId, Count = reward.Count, Slot = -1 };
                    player.Inventory.Add(rewardItem);
                }
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([rewardItem]), ct);
            }
        }

        // Award fixed reward items (always given, no selection)
        var fixedItems = rewards?.RewardItems;
        if (fixedItems is { Count: > 0 })
        {
            var granted = new List<Item>();
            foreach (var reward in fixedItems)
            {
                int maxStk = _dataManager.Items.GetTemplate(reward.ItemId)?.MaxStackCount ?? 1;
                if (!player.Inventory.CanReceive(reward.ItemId, maxStk)) continue;

                var existed = player.Inventory.FindByItemId(reward.ItemId);
                if (existed is not null)
                {
                    existed.Count += reward.Count;
                    granted.Add(existed);
                }
                else
                {
                    long uid = await _itemDao.NextUniqueIdAsync(ct);
                    var item = new Item { UniqueId = uid, ItemId = reward.ItemId, Count = reward.Count, Slot = -1 };
                    player.Inventory.Add(item);
                    granted.Add(item);
                }
            }
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            if (granted.Count > 0)
                await conn.SendAsync(new SM_INVENTORY_ADD_ITEM(granted), ct);
        }

        // Bonus-type hook (Java QuestService.getRewardItems' <bonus> branch): only the first declared
        // <bonus> is read. Dispatches onBonusApplyEvent to any quest registered against this bonus type
        // (e.g. event quests 80016/80018 for MOVIE) and, unless a handler reports FAILED, grants the
        // BonusService.getQuestBonus extra item.
        if (template.BonusList.Count > 0 && template.BonusList[0].Type != "NONE")
        {
            string bonusType = template.BonusList[0].Type;
            var bonusResult = await _questEngine.OnBonusApplyAsync(player, bonusType, conn, ct);
            if (bonusResult != HandlerResult.Failed)
            {
                var bonusItem = GetQuestBonusItem(bonusType);
                if (bonusItem is { } bi && _dataManager.Items.GetTemplate(bi.ItemId) is { } bonusTemplate
                    && player.Inventory.CanReceive(bi.ItemId, bonusTemplate.MaxStackCount))
                {
                    var existedBonus = player.Inventory.FindByItemId(bi.ItemId);
                    Item bonusGranted;
                    if (existedBonus is not null)
                    {
                        existedBonus.Count += bi.Count;
                        bonusGranted = existedBonus;
                    }
                    else
                    {
                        long uid = await _itemDao.NextUniqueIdAsync(ct);
                        bonusGranted = new Item { UniqueId = uid, ItemId = bi.ItemId, Count = bi.Count, Slot = -1 };
                        player.Inventory.Add(bonusGranted);
                    }
                    await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                    await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([bonusGranted]), ct);
                }
            }
        }

        // Award kinah (gold attribute)
        long gold = rewards?.Gold ?? 0;
        if (_rates.QuestKinahRate != 1.0f)
            gold = (long)(gold * _rates.QuestKinahRate);
        if (gold > 0)
        {
            var kinah = player.Inventory.FindByItemId(KinahItemId);
            if (kinah is not null)
            {
                kinah.Count += gold;
            }
            else
            {
                long uid = await _itemDao.NextUniqueIdAsync(ct);
                kinah = new Item { UniqueId = uid, ItemId = KinahItemId, Count = gold, Slot = -1 };
                player.Inventory.Add(kinah);
            }
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        }

        // Award abyss points
        int apReward = rewards?.RewardAbyssPoint ?? 0;
        if (apReward > 0)
        {
            bool questRankUp = AbyssRankService.AddAp(player, apReward);
            await conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), ct);
            await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, player.AbyssGp, player.AbyssTopRanking, ct);
            if (questRankUp)
            {
                var rankUpdatePkt = new SM_ABYSS_RANK_UPDATE(player.ObjectId, player.AbyssRank);
                try { await conn.SendAsync(rankUpdatePkt, ct); } catch { }
                int rankWorldId = player.Position.WorldId;
                foreach (var c in _connRegistry.GetAll())
                {
                    if (c == conn || c.ActivePlayer?.Position.WorldId != rankWorldId) continue;
                    try { await c.SendAsync(rankUpdatePkt, ct); } catch { }
                }
            }
        }

        // Award title (auto-equip; mirrors Java TitleList.addTitle(id, true, 0))
        int titleReward = rewards?.Title ?? -1;
        if (titleReward >= 0)
        {
            player.TitleId = titleReward;
            await _playerDao.UpdateTitleAsync(player.ObjectId, titleReward, ct);
            await conn.SendAsync(SM_TITLE_INFO.ActiveTitle(titleReward), ct);
            int titleWorldId = player.Position.WorldId;
            foreach (var c in _connRegistry.GetAll())
            {
                if (c == conn || c.ActivePlayer?.Position.WorldId != titleWorldId) continue;
                try { await c.SendAsync(SM_TITLE_INFO.BroadcastTitle(player.ObjectId, titleReward), ct); } catch { }
            }
        }

        // Mark complete
        entry.Status        = QuestStatus.COMPLETE;
        entry.CompleteCount = Math.Min(entry.CompleteCount + 1, 127);
        await _questDao.UpsertAsync(player.ObjectId, entry, ct);

        await conn.SendAsync(new SM_QUEST_ACTION(entry.QuestId,
            SM_QUEST_ACTION.ActionType.StepUpdate, (byte)QuestStatus.COMPLETE, entry.Step), ct);
        await conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
        await conn.SendAsync(new SM_QUEST_COMPLETED_LIST(player.Quests.Completed), ct);

        _log.LogInformation("Player {Name} completed quest {QuestId} ({QuestName}), exp={Exp}",
            player.Name, entry.QuestId, template.Name, expReward);

        return true;
    }

    /// <summary>
    /// Java BonusService.getQuestBonus: rolls an extra reward item from an item_groups.xml group
    /// keyed by bonus type (TASK -> craft groups, MANASTONE -> manastone groups, MEDAL -> medal
    /// groups by bonus level; BOSS/MOVIE/etc. return null in Java too). item_groups.xml and its
    /// BonusItemGroup/CraftGroup/ManastoneGroup/MedalGroup data holders are not ported (see
    /// migration_plan.md), so this always returns null — a documented no-op. This does not regress
    /// the quests this hook currently unblocks (80016/80018/80034-80039), which all use bonus type
    /// MOVIE or LUNAR: Java's own getQuestBonus returns null for MOVIE, and LUNAR falls into Java's
    /// "not implemented" default branch (also null) — so those quests never granted a bonus item in
    /// Java either. Only the onBonusApplyEvent side effects (e.g. playing a movie) matter for them.
    /// </summary>
    private static (int ItemId, long Count)? GetQuestBonusItem(string bonusType) => null;
}

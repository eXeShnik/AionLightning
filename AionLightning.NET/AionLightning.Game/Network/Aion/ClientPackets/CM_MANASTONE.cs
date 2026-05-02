using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client enchants or sockets a manastone into gear. Opcode 0x2E8.
/// actionType=1 → enchantment stone (increase enchant level, no failure).
/// actionType=2 → manastone socket (consume stone, record in item_stones; no stat bonus applied).
/// actionType=3 → remove manastone (stub; requires NPC proximity + kinah).
/// </summary>
public sealed class CM_MANASTONE : AionClientPacket
{
    private const byte  MaxEnchantLevel   = 15;
    private const float BaseEnchantChance = 60f; // Java EnchantsConfig.ENCHANT_STONE default
    private const int   MaxManastoneSlots = 6;   // simplified cap; real cap comes from item template
    private const int   KinahItemId       = 182400001;
    private const long  RemovalCost       = 20_000L; // Java PricesService.getPriceForService(500)

    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;
    private readonly IManastoneDao      _manastoneDao;
    private readonly IDataManager       _dataManager;

    private byte _actionType;
    private byte _targetFusedSlot;
    private int  _targetUniqueId;
    private int  _stoneUniqueId;
    private byte _slotNum;

    public CM_MANASTONE(GsClientConnection conn, IItemDao itemDao, IManastoneDao manastoneDao, IDataManager dataManager)
    {
        _conn         = conn;
        _itemDao      = itemDao;
        _manastoneDao = manastoneDao;
        _dataManager  = dataManager;
    }

    public override void Read(ref PacketReader r)
    {
        _actionType     = (byte)r.ReadC();
        _targetFusedSlot = (byte)r.ReadC();
        _targetUniqueId = r.ReadD();
        switch (_actionType)
        {
            case 1:
            case 2:
                _stoneUniqueId = r.ReadD();
                r.ReadD(); // supplementUniqueId (blessing stone) — ignored
                break;
            case 3:
                _slotNum = (byte)r.ReadC();
                r.ReadC(); // pad
                r.ReadH(); // pad
                r.ReadD(); // npcObjId (proximity check deferred — NPC lookup requires World access)
                break;
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_actionType)
        {
            case 1: // enchantment stone — success chance decreases with enchant level
            {
                var target = player.Inventory.Get(_targetUniqueId)
                          ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _targetUniqueId);
                var stone  = player.Inventory.Get(_stoneUniqueId);
                if (target is null || stone is null || stone.UniqueId == target.UniqueId) return;
                if (target.EnchantLevel >= MaxEnchantLevel) return;

                // Success rate: 60% base, −5% per current enchant level, min 5% (mirrors Java EnchantService)
                float successRate = Math.Max(5f, BaseEnchantChance - target.EnchantLevel * 5f);
                bool success = Random.Shared.NextSingle() * 100f < successRate;

                if (success)
                    target.EnchantLevel++;
                else if (target.EnchantLevel > 10)
                    target.EnchantLevel = 10;
                else if (target.EnchantLevel > 0)
                    target.EnchantLevel--;

                stone.Count--;
                if (stone.Count <= 0)
                {
                    player.Inventory.Remove(stone.UniqueId);
                    await _itemDao.DeleteAsync(stone.UniqueId, ct);
                    await _conn.SendAsync(new SM_DELETE_ITEM((int)stone.UniqueId), ct);
                }
                else
                {
                    await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([stone]), ct);
                }

                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([target]), ct);

                string itemTag = target.UniqueId.ToString();
                await _conn.SendAsync(success
                    ? SM_SYSTEM_MESSAGE.EnchantSuccess(itemTag, target.EnchantLevel)
                    : SM_SYSTEM_MESSAGE.EnchantFailed(itemTag), ct);
                break;
            }

            case 2: // manastone socketing — consume stone, record in item_stones; no stat bonus in this implementation
            {
                var target = player.Inventory.Get(_targetUniqueId)
                          ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _targetUniqueId);
                var stone  = player.Inventory.Get(_stoneUniqueId);
                if (target is null || stone is null) return;
                if (target.ManaStones.Count >= MaxManastoneSlots) return;

                var usedSlots = target.ManaStones.Select(m => m.Slot).ToHashSet();
                int nextSlot  = Enumerable.Range(0, MaxManastoneSlots).First(s => !usedSlots.Contains(s));

                var manaStone = new Manastone { ItemUniqueId = target.UniqueId, ItemId = stone.ItemId, Slot = nextSlot };
                target.ManaStones.Add(manaStone);
                await _manastoneDao.InsertAsync(manaStone, ct);

                stone.Count--;
                if (stone.Count <= 0)
                {
                    player.Inventory.Remove(stone.UniqueId);
                    await _itemDao.DeleteAsync(stone.UniqueId, ct);
                    await _conn.SendAsync(new SM_DELETE_ITEM((int)stone.UniqueId), ct);
                }
                else
                {
                    await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([stone]), ct);
                }

                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([target]), ct);
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.ManastoneSuccess(stone.UniqueId.ToString()), ct);

                if (target.IsEquipped)
                    await RecomputeAndSendStatsAsync(player, ct);
                break;
            }

            case 3: // remove manastone — deduct kinah, delete from item_stones, refresh client
            {
                if (_targetFusedSlot != 1) return; // fusionstone removal not implemented

                var target = player.Inventory.Get(_targetUniqueId)
                          ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _targetUniqueId);
                if (target is null) return;

                var stone = target.ManaStones.FirstOrDefault(m => m.Slot == _slotNum);
                if (stone is null) return;

                var kinahItem = player.Inventory.FindByItemId(KinahItemId, includeEquipped: true);
                if (kinahItem is null || kinahItem.Count < RemovalCost)
                {
                    await _conn.SendAsync(SM_SYSTEM_MESSAGE.NoEnoughKinah(), ct);
                    return;
                }

                kinahItem.Count -= RemovalCost;
                if (kinahItem.Count == 0)
                {
                    player.Inventory.Remove(kinahItem.UniqueId);
                    await _itemDao.DeleteAsync(kinahItem.UniqueId, ct);
                    await _conn.SendAsync(new SM_DELETE_ITEM((int)kinahItem.UniqueId), ct);
                }
                else
                {
                    await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinahItem]), ct);
                }

                target.ManaStones.Remove(stone);
                await _manastoneDao.DeleteByItemAndSlotAsync(target.UniqueId, _slotNum, ct);
                await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([target]), ct);

                if (target.IsEquipped)
                    await RecomputeAndSendStatsAsync(player, ct);
                break;
            }
        }
    }

    private async ValueTask RecomputeAndSendStatsAsync(Model.Player player, CancellationToken ct)
    {
        var equipStats = EquipStatsCalculator.Compute(player.Inventory.All.Where(i => i.IsEquipped), _dataManager);
        player.PhysicalDefense             = equipStats.PhysicalDefense;
        player.MagicDefense                = equipStats.MagicDefense;
        player.BonusMaxHp                  = equipStats.BonusMaxHp;
        player.BonusMaxMp                  = equipStats.BonusMaxMp;
        player.BonusPhysicalAtk            = equipStats.PhysicalAttackBonus;
        player.BonusMagicResist            = equipStats.MagicResistBonus;
        player.BonusMagicAtk               = equipStats.MagicAttackBonus;
        player.BonusEvasion                = equipStats.Evasion;
        player.BonusPhysicalAccuracy       = equipStats.PhysicalAccuracy;
        player.BonusPhysicalCritical       = equipStats.PhysicalCritical;
        player.BonusPhysicalCriticalResist = equipStats.PhysicalCriticalResist;
        player.BonusMagicalAccuracy        = equipStats.MagicalAccuracy;
        player.BonusMagicalCritical        = equipStats.MagicalCritical;
        player.BonusMagicalCriticalResist  = equipStats.MagicalCriticalResist;
        player.BonusAttackSpeedPct         = equipStats.AttackSpeedBonus;
        player.BonusConcentration          = equipStats.Concentration;
        player.BonusMagicBoost             = equipStats.MagicBoost;
        player.BonusMagicSuppression       = equipStats.MagicSuppression;
        player.BonusHealBoost              = equipStats.HealBoost;
        player.BonusParry                  = equipStats.Parry;
        player.BonusBlock                  = equipStats.Block;
        player.BonusStrikeFortitude        = equipStats.StrikeFortitude;
        player.BonusSpellFortitude         = equipStats.SpellFortitude;
        player.BonusMovementSpeedPct       = equipStats.MovementSpeedBonus;
        player.BonusFlySpeedPct            = equipStats.FlySpeedBonus;
        player.CurrentAttackSpeed = player.BonusAttackSpeedPct > 0
            ? player.BaseAttackSpeed * 1000 / (1000 + player.BonusAttackSpeedPct)
            : player.BaseAttackSpeed;

        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        player.MovementSpeed = (statTpl?.RunSpeed ?? 6.0f) * (1000 + player.BonusMovementSpeedPct) / 1000f;
        player.MaxHp = (statTpl?.MaxHp ?? 1000) + player.BonusMaxHp + player.TitleBonusMaxHp;
        player.MaxMp = (statTpl?.MaxMp ?? 500)  + player.BonusMaxMp + player.TitleBonusMaxMp;
        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);
    }
}

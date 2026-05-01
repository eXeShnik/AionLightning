using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client combines two enchantment stones into a composite stone using a Combination Tool. Opcode 0x192.
/// Enchantment stones: itemId 166000001–166000095. Combination Tool: itemId 165010000.
/// Output itemId = 166000000 + calcLevel(stone1.level, stone2.level).
/// </summary>
public sealed class CM_COMPOSITE_STONES : AionClientPacket
{
    // Enchantment stones are itemId 166000001..166000095 (level = itemId − 166000000)
    private const int EnchantStoneBase = 166000000;
    private const int EnchantStoneMax  = 95;
    // Combination Tool itemId (Combination Tool, category=COMBINATION)
    private const int CombinationToolId = 165010000;

    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;

    private int _toolUniqueId;
    private int _firstUniqueId;
    private int _secondUniqueId;

    public CM_COMPOSITE_STONES(GsClientConnection conn, IItemDao itemDao)
    {
        _conn    = conn;
        _itemDao = itemDao;
    }

    public override void Read(ref PacketReader r)
    {
        _toolUniqueId   = r.ReadD();
        _firstUniqueId  = r.ReadD();
        _secondUniqueId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        var tool   = player.Inventory.Get(_toolUniqueId);
        var first  = player.Inventory.Get(_firstUniqueId);
        var second = player.Inventory.Get(_secondUniqueId);

        if (tool is null || first is null || second is null) return;
        if (tool.ItemId != CombinationToolId || tool.Count < 1) return;

        // Both inputs must be enchantment stones at level ≤ 95
        int firstLevel  = first.ItemId  - EnchantStoneBase;
        int secondLevel = second.ItemId - EnchantStoneBase;
        if (firstLevel  < 1 || firstLevel  > EnchantStoneMax) return;
        if (secondLevel < 1 || secondLevel > EnchantStoneMax) return;
        if (first.Count < 1 || second.Count < 1) return;

        // Inventory space check: if the output stone is a new itemId, need a free slot
        int outputLevel = CalcOutputLevel(firstLevel, secondLevel);
        int outputItemId = EnchantStoneBase + outputLevel;
        var existingOutput = player.Inventory.FindByItemId(outputItemId);
        if (existingOutput is null && !player.Inventory.HasFreeSlot)
        {
            await _conn.SendAsync(SM_SYSTEM_MESSAGE.InventoryFull(), ct);
            return;
        }

        // Consume 1 tool, 1 of each stone
        tool.Count--;
        if (tool.Count <= 0)
        {
            player.Inventory.Remove(tool.UniqueId);
            await _itemDao.DeleteAsync(tool.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(tool.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([tool]), ct);
        }

        first.Count--;
        if (first.Count <= 0)
        {
            player.Inventory.Remove(first.UniqueId);
            await _itemDao.DeleteAsync(first.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(first.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([first]), ct);
        }

        second.Count--;
        if (second.Count <= 0)
        {
            player.Inventory.Remove(second.UniqueId);
            await _itemDao.DeleteAsync(second.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(second.UniqueId), ct);
        }
        else
        {
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([second]), ct);
        }

        // Create or stack the output composite stone
        Item output;
        if (existingOutput is not null)
        {
            existingOutput.Count++;
            output = existingOutput;
        }
        else
        {
            long uid = await _itemDao.NextUniqueIdAsync(ct);
            output = new Item { UniqueId = uid, ItemId = outputItemId, Count = 1, Slot = -1 };
            player.Inventory.Add(output);
        }

        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([output]), ct);
    }

    // Mirrors Java CompositionAction.calcLevel: average of two levels with random jitter
    private static int CalcOutputLevel(int first, int second)
    {
        int avg = (first + second) / 2;
        if (avg < 11)
            return Math.Clamp(Random.Shared.Next(1, 21), 1, EnchantStoneMax);

        int jitter = Random.Shared.Next(1, 11);
        int sign   = Random.Shared.Next(0, 2) == 0 ? -1 : 1;
        return Math.Clamp(avg + sign * jitter, 1, EnchantStoneMax);
    }
}

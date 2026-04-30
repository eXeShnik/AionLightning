using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client attempts to enchant or socket a manastone into gear. Opcode 0x2E8.
/// enchantType=0 → enchantment stone (increase enchant level, no failure).
/// enchantType=1 → manastone socket (not yet implemented).
/// </summary>
public sealed class CM_MANASTONE : AionClientPacket
{
    private const byte MaxEnchantLevel = 15;

    private readonly GsClientConnection _conn;
    private readonly IItemDao _itemDao;

    private byte _enchantType;
    private int  _targetUniqueId;
    private int  _stoneUniqueId;

    public CM_MANASTONE(GsClientConnection conn, IItemDao itemDao)
    {
        _conn    = conn;
        _itemDao = itemDao;
    }

    public override void Read(ref PacketReader r)
    {
        _enchantType    = (byte)r.ReadC();
        r.ReadC();               // targetFusedSlot (0=main, 1=fused — not yet used)
        _targetUniqueId = r.ReadD();
        _stoneUniqueId  = r.ReadD();
        r.ReadD();               // supplementUniqueId (blessing stone — ignored)
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        // Only handle enchant stones (type 0); manastone socketing (type 1) is a no-op
        if (_enchantType != 0) return;

        var player = _conn.ActivePlayer;
        if (player is null) return;

        var target = player.Inventory.Get(_targetUniqueId);
        var stone  = player.Inventory.Get(_stoneUniqueId);
        if (target is null || stone is null || stone.UniqueId == target.UniqueId) return;
        if (target.EnchantLevel >= MaxEnchantLevel) return;

        // Increase enchant level (simplified: always succeeds)
        target.EnchantLevel++;

        // Consume the enchant stone
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

        // Persist and notify client of updated target item
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([target]), ct);
    }
}

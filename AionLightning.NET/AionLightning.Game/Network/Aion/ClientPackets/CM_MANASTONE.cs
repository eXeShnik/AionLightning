using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Client enchants or sockets a manastone into gear. Opcode 0x2E8.
/// actionType=1 → enchantment stone (increase enchant level, no failure).
/// actionType=2 → manastone socket (consume stone; stat effect simplified — no actual bonus applied).
/// actionType=3 → remove manastone (stub; requires NPC proximity + kinah).
/// </summary>
public sealed class CM_MANASTONE : AionClientPacket
{
    private const byte MaxEnchantLevel = 15;

    private readonly GsClientConnection _conn;
    private readonly IItemDao _itemDao;

    private byte _actionType;
    private byte _targetFusedSlot;
    private int  _targetUniqueId;
    private int  _stoneUniqueId;

    public CM_MANASTONE(GsClientConnection conn, IItemDao itemDao)
    {
        _conn    = conn;
        _itemDao = itemDao;
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
                r.ReadC(); // slotNum
                r.ReadC(); // pad
                r.ReadH(); // pad
                r.ReadD(); // npcObjId
                break;
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_actionType)
        {
            case 1: // enchantment stone — increase enchant level, always succeeds
            {
                var target = player.Inventory.Get(_targetUniqueId)
                          ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _targetUniqueId);
                var stone  = player.Inventory.Get(_stoneUniqueId);
                if (target is null || stone is null || stone.UniqueId == target.UniqueId) return;
                if (target.EnchantLevel >= MaxEnchantLevel) return;

                target.EnchantLevel++;

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
                break;
            }

            case 2: // manastone socketing — consume stone, no stat bonus in this implementation
            {
                var target = player.Inventory.Get(_targetUniqueId)
                          ?? player.Inventory.All.FirstOrDefault(i => i.IsEquipped && i.UniqueId == _targetUniqueId);
                var stone  = player.Inventory.Get(_stoneUniqueId);
                if (target is null || stone is null) return;

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
                await _conn.SendAsync(SM_SYSTEM_MESSAGE.ManastoneSuccess(stone.UniqueId.ToString()), ct);
                break;
            }

            // case 3: remove manastone — stub (requires NPC proximity + kinah; not yet implemented)
        }
    }
}

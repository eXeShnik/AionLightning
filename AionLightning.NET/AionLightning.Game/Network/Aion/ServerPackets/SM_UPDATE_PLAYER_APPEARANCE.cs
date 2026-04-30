using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a player's visual equipment state to all players in range. Opcode 0x24.</summary>
public sealed class SM_UPDATE_PLAYER_APPEARANCE : AionServerPacket
{
    private readonly int _playerObjectId;
    private readonly IReadOnlyList<Item> _equipped;

    public SM_UPDATE_PLAYER_APPEARANCE(int playerObjectId, IEnumerable<Item> allItems) : base(0x24)
    {
        _playerObjectId = playerObjectId;
        _equipped = allItems.Where(i => i.IsEquipped).ToList();
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerObjectId);

        // Equipment slot mask: OR of all equipped slot values
        int mask = _equipped.Aggregate(0, (acc, item) => acc | item.Slot);
        w.WriteD(mask);

        foreach (var item in _equipped)
        {
            w.WriteD(item.ItemId); // skin template ID — use itemId directly
            w.WriteD(0);           // godstone itemId (none)
            w.WriteD(0);           // item color (default)
            w.WriteD(item.EnchantLevel >= 15 ? 1 : 0); // enchant glow
        }
    }
}

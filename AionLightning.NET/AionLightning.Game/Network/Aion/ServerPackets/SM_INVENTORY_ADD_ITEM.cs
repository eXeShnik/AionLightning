using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies the client that one or more items were added or updated in the inventory. Opcode 0x1B.</summary>
public sealed class SM_INVENTORY_ADD_ITEM : AionServerPacket
{
    private readonly IReadOnlyList<Item> _items;
    private readonly short _mask; // 0 = ITEM_COLLECT (loot), 4 = BUY

    public SM_INVENTORY_ADD_ITEM(IEnumerable<Item> items, short mask = 0) : base(0x1B)
    {
        _items = items as IReadOnlyList<Item> ?? items.ToList();
        _mask  = mask;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(_mask);
        w.WriteH((short)_items.Count);

        foreach (var item in _items)
            SM_INVENTORY_INFO.WriteItemInfo(ref w, item);
    }
}

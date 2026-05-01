using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends legion warehouse contents to the client. Opcode 0x1A, storage-type byte 4.
/// Uses the same item blob layout as SM_WAREHOUSE_INFO.
/// </summary>
public sealed class SM_LEGION_WAREHOUSE_INFO : AionServerPacket
{
    private readonly IReadOnlyList<Item> _items;

    public SM_LEGION_WAREHOUSE_INFO(IEnumerable<Item> items) : base(0x1A)
        => _items = items as IReadOnlyList<Item> ?? items.ToList();

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(4); // storage type: legion warehouse
        w.WriteC(0); // firstPacket
        w.WriteC(1); // warehouse expand level
        w.WriteC(0); // unk
        w.WriteH((short)_items.Count);

        foreach (var item in _items)
            SM_WAREHOUSE_INFO.WriteItemInfo(ref w, item);
    }
}

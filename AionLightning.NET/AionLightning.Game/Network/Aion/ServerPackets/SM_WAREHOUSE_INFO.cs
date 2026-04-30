using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends personal warehouse contents to the client. Opcode 0x1A.
/// Uses storage-type byte 2 (CUBE / personal warehouse) to distinguish from inventory.
/// Same item blob layout as SM_INVENTORY_INFO.
/// </summary>
public sealed class SM_WAREHOUSE_INFO : AionServerPacket
{
    private readonly IReadOnlyList<Item> _items;

    public SM_WAREHOUSE_INFO(IEnumerable<Item> items) : base(0x1A)
        => _items = items as IReadOnlyList<Item> ?? items.ToList();

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(2); // storage type: CUBE (personal warehouse)
        w.WriteC(0); // npcExpandsSize
        w.WriteC(0); // questExpandsSize
        w.WriteC(0); // unk
        w.WriteH((short)_items.Count);

        foreach (var item in _items)
            WriteItemInfo(ref w, item);
    }

    internal static void WriteItemInfo(ref PacketWriter w, Item item)
    {
        w.WriteD((int)item.UniqueId);
        w.WriteD(item.ItemId);
        w.WriteD(0); // nameId

        short itemMask = (short)(item.EnchantLevel > 0 ? 0x40 : 0);

        w.WriteC(0x00);       // blob type: GENERAL_INFO
        w.WriteH(itemMask);
        w.WriteQ(item.Count);
        w.WriteC(0); w.WriteC(0); // creator name: empty UTF-16LE
        w.WriteC(0);
        w.WriteD(0); w.WriteD(0); w.WriteD(0);
        w.WriteH(0);
        w.WriteD(0);

        w.WriteH(item.Slot);
        w.WriteC(0); // cloth flag

        if (item.EnchantLevel > 0)
            w.WriteC(item.EnchantLevel); // ENCHANT_INFO blob content
    }
}

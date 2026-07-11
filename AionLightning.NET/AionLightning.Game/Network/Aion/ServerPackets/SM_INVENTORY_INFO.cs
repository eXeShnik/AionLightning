using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends the player's bag contents to the client. Mirrors Java SM_INVENTORY_INFO.
/// Uses a GENERAL_INFO blob per item, with an optional ENCHANT_INFO blob (mask 0x40)
/// when the item has a non-zero enchant level.
/// </summary>
public sealed class SM_INVENTORY_INFO : AionServerPacket
{
    private readonly bool _isFirst;
    private readonly IReadOnlyList<Item> _items;
    private readonly byte _npcExpands;
    private readonly byte _questExpands;

    public SM_INVENTORY_INFO(bool isFirst, IEnumerable<Item> items,
        byte npcExpands = 0, byte questExpands = 0) : base(0x1A)
    {
        _isFirst      = isFirst;
        _items        = items as IReadOnlyList<Item> ?? items.ToList();
        _npcExpands   = npcExpands;
        _questExpands = questExpands;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_isFirst ? (byte)1 : (byte)0);
        w.WriteC(_npcExpands);   // npcExpandsSize
        w.WriteC(_questExpands); // questExpandsSize
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
        WriteItemBlob(ref w, item);
    }

    internal static void WriteItemBlob(ref PacketWriter w, Item item)
    {
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

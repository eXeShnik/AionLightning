using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the item list for an open loot window. Opcode 0xCE.</summary>
public sealed class SM_LOOT_ITEMLIST : AionServerPacket
{
    public readonly record struct LootItem(int ItemId, long Count, bool IsTradeable, byte Socket = 0);

    private readonly int _targetObjectId;
    private readonly IReadOnlyList<LootItem> _items;

    public SM_LOOT_ITEMLIST(int targetObjectId, IReadOnlyList<LootItem> items) : base(0xCE)
    {
        _targetObjectId = targetObjectId;
        _items          = items;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_targetObjectId);
        w.WriteC((byte)_items.Count);
        for (byte i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            w.WriteC(i);
            w.WriteD(item.ItemId);
            w.WriteD((int)item.Count);
            w.WriteC(item.Socket);
            w.WriteH(0);
            w.WriteC(0);
            w.WriteC(item.IsTradeable ? (byte)0 : (byte)1); // nonTradeable flag: 1 = not tradeable
        }
    }
}

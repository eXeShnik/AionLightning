using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends account warehouse contents to the client. Opcode 0x1A.
/// Storage-type byte 3 = account warehouse (shared across characters on the same account).
/// </summary>
public sealed class SM_ACCOUNT_WAREHOUSE_INFO : AionServerPacket
{
    private readonly IReadOnlyList<Item> _items;

    public SM_ACCOUNT_WAREHOUSE_INFO(IEnumerable<Item> items) : base(0x1A)
        => _items = items as IReadOnlyList<Item> ?? items.ToList();

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(3); // storage type: account warehouse
        w.WriteC(0); // npcExpandsSize
        w.WriteC(0); // questExpandsSize
        w.WriteC(0); // unk
        w.WriteH((short)_items.Count);

        foreach (var item in _items)
            SM_WAREHOUSE_INFO.WriteItemInfo(ref w, item);
    }
}

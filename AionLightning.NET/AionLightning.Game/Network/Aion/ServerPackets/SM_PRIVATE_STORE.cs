using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends a private store's item listing to a viewing player. Opcode 0x9D.</summary>
public sealed class SM_PRIVATE_STORE : AionServerPacket
{
    private readonly Player _seller;

    public SM_PRIVATE_STORE(Player seller) : base(0x9D)
    {
        _seller = seller;
    }

    public override void Write(ref PacketWriter w)
    {
        var items = _seller.StoreItems ?? [];
        w.WriteD(_seller.ObjectId);
        w.WriteH((short)items.Count);

        foreach (var si in items)
        {
            var inv = _seller.Inventory.Get(si.UniqueId);
            if (inv is null) continue;

            w.WriteD(si.UniqueId);
            w.WriteD(si.ItemId);
            w.WriteH((short)si.Count);
            w.WriteD(si.Price);
            SM_INVENTORY_INFO.WriteItemInfo(ref w, inv);
        }
    }
}

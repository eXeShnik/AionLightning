using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies both trade partners that an item was added to the exchange window. Opcode 0x4B.</summary>
public sealed class SM_EXCHANGE_ADD_ITEM : AionServerPacket
{
    private readonly byte _action; // 0 = self's side, 1 = partner's side
    private readonly Item _item;

    public SM_EXCHANGE_ADD_ITEM(byte action, Item item) : base(0x4B)
    {
        _action = action;
        _item   = item;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_action);
        w.WriteD(_item.ItemId);      // templateId
        w.WriteD((int)_item.UniqueId);
        w.WriteD(0);                 // nameId (not stored in our model)
        // GeneralInfoBlob
        w.WriteC(0x00);
        w.WriteH(0);
        w.WriteQ(_item.Count);
        w.WriteC(0); w.WriteC(0);    // creator empty
        w.WriteC(0);
        w.WriteD(0); w.WriteD(0); w.WriteD(0);
        w.WriteH(0); w.WriteD(0);
        w.WriteH(-1);                // slot
        w.WriteC(0);
    }
}

using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Updates a single existing inventory item in place — stack count changes,
/// stat changes (Java SM_INVENTORY_UPDATE_ITEM). Opcode 0x1D.
/// </summary>
public sealed class SM_INVENTORY_UPDATE_ITEM : AionServerPacket
{
    /// <summary>Java ItemPacketService.ItemUpdateType masks (sendable subset).</summary>
    public enum UpdateType : short
    {
        StatsChange      = 0x00,
        IncItemMerge     = 0x01,
        IncKinahMerge    = 0x05,
        DecItemSplit     = 0x06,
        DecItemSplitMove = 0x0A,
        Put              = 0x13,
        DecItemUse       = 0x16,
        IncItemCollect   = 0x19,
    }

    private readonly Item       _item;
    private readonly UpdateType _updateType;

    public SM_INVENTORY_UPDATE_ITEM(Item item, UpdateType updateType) : base(0x1D)
    {
        _item       = item;
        _updateType = updateType;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD((int)_item.UniqueId);
        // nameId block (Java writeNameId)
        w.WriteH(0x24);
        w.WriteD(0);
        w.WriteH(0);

        SM_INVENTORY_INFO.WriteItemBlob(ref w, _item);

        w.WriteH((short)_updateType);
    }
}

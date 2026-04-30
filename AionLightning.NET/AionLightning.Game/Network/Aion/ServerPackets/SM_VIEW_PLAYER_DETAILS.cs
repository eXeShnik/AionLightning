using AionLightning.Commons.Network;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends another player's equipped items to the inspecting client. Opcode 0x41.
/// UniqueIds are zeroed so the inspector cannot extract item serial numbers.
/// </summary>
public sealed class SM_VIEW_PLAYER_DETAILS : AionServerPacket
{
    private readonly int _targetObjectId;
    private readonly IReadOnlyList<Item> _equipped;

    public SM_VIEW_PLAYER_DETAILS(int targetObjectId, IEnumerable<Item> equippedItems) : base(0x41)
    {
        _targetObjectId = targetObjectId;
        _equipped = equippedItems as IReadOnlyList<Item> ?? equippedItems.ToList();
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_targetObjectId);
        w.WriteC(11); // constant — matches Java writeC(11) in SM_VIEW_PLAYER_DETAILS
        w.WriteH((short)_equipped.Count);

        foreach (var item in _equipped)
            WriteItemEntry(ref w, item);
    }

    private static void WriteItemEntry(ref PacketWriter w, Item item)
    {
        w.WriteD(0);            // uniqueId hidden for privacy
        w.WriteD(item.ItemId);  // template ID
        w.WriteD(0);            // nameId

        short mask = (short)(item.EnchantLevel > 0 ? 0x40 : 0);

        w.WriteC(0x00);         // blob type: GENERAL_INFO
        w.WriteH(mask);
        w.WriteQ(item.Count);
        w.WriteC(0); w.WriteC(0); // creator name: empty null-terminator
        w.WriteC(0);
        w.WriteD(0); w.WriteD(0); w.WriteD(0);
        w.WriteH(0);
        w.WriteD(0);

        w.WriteH(item.Slot);
        w.WriteC(0); // cloth flag

        if (item.EnchantLevel > 0)
            w.WriteC(item.EnchantLevel);
    }
}

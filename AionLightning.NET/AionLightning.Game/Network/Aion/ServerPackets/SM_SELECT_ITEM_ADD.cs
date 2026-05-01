using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Confirms the player's selection from a reward box. Opcode 0x11E.</summary>
public sealed class SM_SELECT_ITEM_ADD : AionServerPacket
{
    private readonly int _uniqueItemId;
    private readonly int _index;

    public SM_SELECT_ITEM_ADD(int uniqueItemId, int index) : base(0x11E)
    {
        _uniqueItemId = uniqueItemId;
        _index        = index;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_uniqueItemId);
        w.WriteD(0);
        w.WriteC((byte)_index);
    }
}

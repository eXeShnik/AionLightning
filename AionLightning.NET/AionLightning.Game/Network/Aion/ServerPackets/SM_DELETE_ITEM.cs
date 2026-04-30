using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Tells the client to remove an item from the displayed inventory. Opcode 0x1C.
/// </summary>
public sealed class SM_DELETE_ITEM : AionServerPacket
{
    private readonly long _uniqueId;

    public SM_DELETE_ITEM(long uniqueId) : base(0x1C) => _uniqueId = uniqueId;

    public override void Write(ref PacketWriter w)
    {
        w.WriteD((int)_uniqueId);
        w.WriteC(0); // deleteType mask: 0 = regular player deletion
    }
}

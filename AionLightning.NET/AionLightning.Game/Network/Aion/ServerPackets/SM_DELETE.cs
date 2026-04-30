using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Tells clients to remove an object from view. Opcode 0x16.</summary>
public sealed class SM_DELETE : AionServerPacket
{
    private readonly int _objectId;
    private readonly byte _time; // removal animation speed: 0 = instant, 15 = slow

    public SM_DELETE(int objectId, byte time = 0) : base(0x16)
    {
        _objectId = objectId;
        _time     = time;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteC(_time);
    }
}

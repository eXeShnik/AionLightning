using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends updated heading for a world object (opcode 0x39).</summary>
public sealed class SM_HEADING_UPDATE : AionServerPacket
{
    private readonly int  _objectId;
    private readonly byte _heading;

    public SM_HEADING_UPDATE(int objectId, int heading) : base(0x39)
    {
        _objectId = objectId;
        _heading  = (byte)heading;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteC(_heading);
    }
}

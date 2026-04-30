using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Tells the client to open the teleporter map UI. The client uses teleportId to look up
/// available destinations from its own client-side data. Opcode 0xC4.
/// </summary>
public sealed class SM_TELEPORT_MAP : AionServerPacket
{
    private readonly int _objectId;
    private readonly int _teleportId;

    public SM_TELEPORT_MAP(int objectId, int teleportId) : base(0xC4)
    {
        _objectId   = objectId;
        _teleportId = teleportId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteH(_teleportId);
    }
}

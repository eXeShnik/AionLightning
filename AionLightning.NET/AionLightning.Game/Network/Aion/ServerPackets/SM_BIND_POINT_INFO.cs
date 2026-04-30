using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sends the player's active bind point (obelisk). Opcode 0xEB.</summary>
public sealed class SM_BIND_POINT_INFO : AionServerPacket
{
    private readonly int   _worldId;
    private readonly float _x, _y, _z;

    public SM_BIND_POINT_INFO(Position pos) : base(0xEB)
    {
        _worldId = pos.WorldId;
        _x       = pos.X;
        _y       = pos.Y;
        _z       = pos.Z;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x00); // kisk type: 0 = obelisk (not a kisk)
        w.WriteC(0x01); // unk
        w.WriteD(_worldId);
        w.WriteF(_x);
        w.WriteF(_y);
        w.WriteF(_z);
        w.WriteD(0);    // kisk objectId: 0 = none
    }
}

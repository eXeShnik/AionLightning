using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Broadcasts a creature's visual state (hidden/revealed). Opcode 0x44.</summary>
public sealed class SM_PLAYER_STATE : AionServerPacket
{
    private readonly int  _objectId;
    private readonly byte _visualState;
    private readonly byte _seeState;

    public SM_PLAYER_STATE(int objectId, byte visualState = 0, byte seeState = 0) : base(0x44)
    {
        _objectId    = objectId;
        _visualState = visualState;
        _seeState    = seeState;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteC(_visualState);
        w.WriteC(_seeState);
        w.WriteC(_visualState == 64 ? (byte)0x01 : (byte)0x00);
    }
}

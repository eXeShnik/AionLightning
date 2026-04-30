using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Notifies the client of a player's stance state (opcode 0x1F). State: 0=off, 1=active.</summary>
public sealed class SM_PLAYER_STANCE : AionServerPacket
{
    private readonly int  _objectId;
    private readonly byte _state;

    public SM_PLAYER_STANCE(int objectId, int state) : base(0x1F)
    {
        _objectId = objectId;
        _state    = (byte)state;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_objectId);
        w.WriteC(_state);
    }
}

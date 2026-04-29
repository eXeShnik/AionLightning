using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Cs.ServerPackets;

public sealed class SM_CS_PLAYER_LOGOUT : AionServerPacket
{
    private readonly int _playerId;

    public SM_CS_PLAYER_LOGOUT(int playerId) : base(0x02) => _playerId = playerId;

    public override void Write(ref PacketWriter w) => w.WriteD(_playerId);
}

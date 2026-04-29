using AionLightning.Commons.Network;

namespace AionLightning.Chat.Network.Gs.ServerPackets;

public sealed class SM_PLAYER_AUTH_RESPONSE : AionServerPacket
{
    private readonly int _playerId;
    private readonly byte[] _token;

    public SM_PLAYER_AUTH_RESPONSE(int playerId, byte[] token) : base(0x01)
    {
        _playerId = playerId;
        _token = token;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerId);
        w.WriteC((byte)_token.Length);
        w.WriteB(_token);
    }
}

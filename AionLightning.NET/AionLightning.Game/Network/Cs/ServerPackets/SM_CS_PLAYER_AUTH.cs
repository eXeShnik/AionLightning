using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Cs.ServerPackets;

public sealed class SM_CS_PLAYER_AUTH : AionServerPacket
{
    private readonly int _playerId;
    private readonly string _playerLogin;
    private readonly string _nick;

    public SM_CS_PLAYER_AUTH(int playerId, string playerLogin, string nick) : base(0x01)
    {
        _playerId = playerId;
        _playerLogin = playerLogin;
        _nick = nick;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playerId);
        w.WriteS(_playerLogin);
        w.WriteS(_nick);
    }
}

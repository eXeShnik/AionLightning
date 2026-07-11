using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.ServerPackets;

/// <summary>
/// Hands the game server a one-time key the client can use to fast-reconnect
/// to the login server (logout to character/server select without re-entering password).
/// </summary>
public sealed class SM_ACCOUNT_RECONNECT_KEY : AionServerPacket
{
    private readonly int _accountId;
    private readonly int _reconnectKey;

    public SM_ACCOUNT_RECONNECT_KEY(int accountId, int reconnectKey) : base(0x03)
    {
        _accountId = accountId;
        _reconnectKey = reconnectKey;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_accountId);
        w.WriteD(_reconnectKey);
    }
}

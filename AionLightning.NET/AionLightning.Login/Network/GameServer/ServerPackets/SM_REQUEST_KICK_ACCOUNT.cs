using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.ServerPackets;

public sealed class SM_REQUEST_KICK_ACCOUNT : AionServerPacket
{
    private readonly int _accountId;

    public SM_REQUEST_KICK_ACCOUNT(int accountId) : base(0x02)
    {
        _accountId = accountId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_accountId);
    }
}

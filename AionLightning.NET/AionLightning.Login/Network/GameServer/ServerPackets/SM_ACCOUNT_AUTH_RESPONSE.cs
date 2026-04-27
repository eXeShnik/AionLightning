using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.ServerPackets;

public sealed class SM_ACCOUNT_AUTH_RESPONSE : AionServerPacket
{
    private readonly int _accountId;
    private readonly bool _ok;

    public SM_ACCOUNT_AUTH_RESPONSE(int accountId, bool ok) : base(0x01)
    {
        _accountId = accountId;
        _ok = ok;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(1);
        w.WriteD(_accountId);
        w.WriteC((byte)(_ok ? 1 : 0));
        // TODO M2: write account name, access level, membership, toll when ok
    }
}

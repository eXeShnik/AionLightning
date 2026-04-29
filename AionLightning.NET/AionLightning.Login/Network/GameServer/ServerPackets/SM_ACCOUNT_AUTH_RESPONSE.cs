using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.GameServer.ServerPackets;

public sealed class SM_ACCOUNT_AUTH_RESPONSE : AionServerPacket
{
    private readonly int _accountId;
    private readonly bool _ok;
    private readonly string _accountName;
    private readonly byte _accessLevel;
    private readonly byte _membership;
    private readonly long _toll;

    public SM_ACCOUNT_AUTH_RESPONSE(int accountId, bool ok,
        string accountName = "", byte accessLevel = 0, byte membership = 0, long toll = 0) : base(0x01)
    {
        _accountId = accountId;
        _ok = ok;
        _accountName = accountName;
        _accessLevel = accessLevel;
        _membership = membership;
        _toll = toll;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_accountId);
        w.WriteC((byte)(_ok ? 1 : 0));

        if (_ok)
        {
            w.WriteS(_accountName);
            w.WriteQ(0L);             // accumulatedOnlineTime
            w.WriteQ(0L);             // accumulatedRestTime
            w.WriteC(_accessLevel);
            w.WriteC(_membership);
            w.WriteQ(_toll);
        }
    }
}

using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ServerPackets;

public sealed class SM_LOGIN_OK : AionServerPacket
{
    private readonly int _accountId;
    private readonly int _loginOk;

    public SM_LOGIN_OK(int accountId, int loginOk) : base(0x03)
    {
        _accountId = accountId;
        _loginOk = loginOk;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_accountId);
        w.WriteD(_loginOk);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteD(0x00);
        w.WriteZero(0x19);
    }
}

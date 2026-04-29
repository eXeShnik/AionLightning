using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ServerPackets;

public sealed class SM_UPDATE_SESSION : AionServerPacket
{
    private readonly int _accountId;
    private readonly int _loginOk;

    public SM_UPDATE_SESSION(int accountId, int loginOk) : base(0x0C)
    {
        _accountId = accountId;
        _loginOk = loginOk;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_accountId);
        w.WriteD(_loginOk);
        w.WriteC(0x00);
    }
}

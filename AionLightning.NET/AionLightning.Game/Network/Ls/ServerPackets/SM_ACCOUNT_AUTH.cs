using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Ls.ServerPackets;

public sealed class SM_ACCOUNT_AUTH : AionServerPacket
{
    private readonly int _accountId;
    private readonly int _loginOk;
    private readonly int _playOk1;
    private readonly int _playOk2;

    public SM_ACCOUNT_AUTH(int accountId, int loginOk, int playOk1, int playOk2) : base(0x01)
    {
        _accountId = accountId;
        _loginOk   = loginOk;
        _playOk1   = playOk1;
        _playOk2   = playOk2;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_accountId);
        w.WriteD(_loginOk);
        w.WriteD(_playOk1);
        w.WriteD(_playOk2);
    }
}

using AionLightning.Commons.Network;

namespace AionLightning.Login.Network.Aion.ServerPackets;

public sealed class SM_PLAY_OK : AionServerPacket
{
    private readonly int _playOk1;
    private readonly int _playOk2;
    private readonly byte _serverId;

    public SM_PLAY_OK(int playOk1, int playOk2, byte serverId) : base(0x07)
    {
        _playOk1 = playOk1;
        _playOk2 = playOk2;
        _serverId = serverId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_playOk1);
        w.WriteD(_playOk2);
        w.WriteC(_serverId);
    }
}

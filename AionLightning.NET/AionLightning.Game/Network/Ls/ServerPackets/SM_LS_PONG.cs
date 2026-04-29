using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Ls.ServerPackets;

public sealed class SM_LS_PONG : AionServerPacket
{
    private readonly byte _gsId;

    public SM_LS_PONG(byte gsId) : base(0x0C)
    {
        _gsId = gsId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(_gsId);
        w.WriteD(Environment.ProcessId);
    }
}

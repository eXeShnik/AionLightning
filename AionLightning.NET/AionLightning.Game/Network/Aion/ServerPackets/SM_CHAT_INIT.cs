using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_CHAT_INIT : AionServerPacket
{
    private readonly byte[] _token;

    public SM_CHAT_INIT(byte[] token) : base(0xE6) => _token = token;

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_token.Length);
        w.WriteB(_token);
    }
}

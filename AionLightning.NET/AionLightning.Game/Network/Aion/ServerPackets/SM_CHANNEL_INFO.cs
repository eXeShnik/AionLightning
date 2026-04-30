using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Tells the client which channel (instance) the player is in.
/// For open-world maps: channel 0, 1 available instance.
/// Opcode 0xE5.
/// </summary>
public sealed class SM_CHANNEL_INFO : AionServerPacket
{
    public SM_CHANNEL_INFO() : base(0xE5) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(0); // currentChannel (0-based; open world = 0)
        w.WriteD(1); // instanceCount (number of channels available)
    }
}

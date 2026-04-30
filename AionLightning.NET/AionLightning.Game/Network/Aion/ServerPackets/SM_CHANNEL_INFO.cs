using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Tells the client which channel (instance) the player is in.
/// Opcode 0xE5.
/// </summary>
public sealed class SM_CHANNEL_INFO : AionServerPacket
{
    private readonly int _currentChannel;
    private readonly int _instanceCount;

    public SM_CHANNEL_INFO(int currentChannel = 0, int instanceCount = 1) : base(0xE5)
    {
        _currentChannel = currentChannel;
        _instanceCount  = instanceCount;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_currentChannel);
        w.WriteD(_instanceCount);
    }
}

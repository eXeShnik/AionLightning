using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends the LS reconnect key to the Aion client so it can re-authenticate at LoginServer.
/// Connection is closed after this packet is sent. Opcode 0xFF.
/// </summary>
public sealed class SM_RECONNECT_KEY : AionServerPacket
{
    private readonly int _key;

    public SM_RECONNECT_KEY(int key) : base(0xFF) => _key = key;

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0x00);
        w.WriteD(_key);
    }
}

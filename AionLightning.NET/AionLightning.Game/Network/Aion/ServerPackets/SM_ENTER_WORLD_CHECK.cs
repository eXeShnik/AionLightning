using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Synchronisation checkpoint packet sent before sending inventory/stats.
/// Java comment: "dunno wtf this packet is doing." — opcode 0x0D, 3 bytes.
/// </summary>
public sealed class SM_ENTER_WORLD_CHECK : AionServerPacket
{
    public SM_ENTER_WORLD_CHECK() : base(0x0D) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC(0);
        w.WriteC(0);
        w.WriteC(0);
    }
}

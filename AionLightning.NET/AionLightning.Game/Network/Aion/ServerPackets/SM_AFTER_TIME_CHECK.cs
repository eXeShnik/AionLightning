using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>Sent after time-check handshake completes. Opcode 0x124.</summary>
public sealed class SM_AFTER_TIME_CHECK : AionServerPacket
{
    public SM_AFTER_TIME_CHECK() : base(0x124) { }

    public override void Write(ref PacketWriter w)
    {
        w.WriteH(1);
        w.WriteD(0);
    }
}

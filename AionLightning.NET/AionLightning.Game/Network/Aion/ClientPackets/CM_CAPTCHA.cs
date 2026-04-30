using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client submits captcha answer. Stub — opcode 0xAC.</summary>
public sealed class CM_CAPTCHA : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        byte type = (byte)r.ReadC();
        if (type == 0x00 || type == 0x01)
        {
            r.ReadC(); // count
            r.ReadS(); // word
        }
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

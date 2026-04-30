using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client plays a character motion/emote animation. Stub — opcode 0x2E5.</summary>
public sealed class CM_MOTION : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadH(); // motionId
        r.ReadC(); // speed flag
    }

    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

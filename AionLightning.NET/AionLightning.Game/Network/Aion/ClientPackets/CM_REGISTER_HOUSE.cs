using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client registers to bid on a house. Stub — opcode 0x1B2.</summary>
public sealed class CM_REGISTER_HOUSE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadQ(); // bidKinah
        r.ReadQ(); // unk1 (always 100000)
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

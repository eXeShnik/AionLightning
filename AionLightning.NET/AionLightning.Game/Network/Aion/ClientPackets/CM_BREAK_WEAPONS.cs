using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client breaks a fused weapon apart. Stub — opcode 0x16D.</summary>
public sealed class CM_BREAK_WEAPONS : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // unk
        r.ReadD(); // weaponToBreakUniqueId
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

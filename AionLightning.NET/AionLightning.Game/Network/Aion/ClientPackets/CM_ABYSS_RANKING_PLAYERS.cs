using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests the abyss player ranking. Stub — opcode 0x19E.</summary>
public sealed class CM_ABYSS_RANKING_PLAYERS : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadC(); // raceId (0=Elyos, 1=Asmo)
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

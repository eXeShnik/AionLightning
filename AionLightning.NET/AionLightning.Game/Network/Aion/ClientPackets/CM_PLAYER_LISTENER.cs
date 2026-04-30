using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client registers a player observer. Stub — opcode 0xCA.</summary>
public sealed class CM_PLAYER_LISTENER : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadC();
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

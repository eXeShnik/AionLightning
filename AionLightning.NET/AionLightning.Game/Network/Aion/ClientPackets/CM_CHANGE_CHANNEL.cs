using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client changes the zone channel. Stub — opcode 0x14E.</summary>
public sealed class CM_CHANGE_CHANNEL : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadD(); // channelId
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

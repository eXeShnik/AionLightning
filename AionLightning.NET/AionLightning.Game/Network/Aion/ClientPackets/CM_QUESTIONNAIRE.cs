using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client submits an in-game questionnaire. Stub — opcode 0x153.</summary>
public sealed class CM_QUESTIONNAIRE : AionClientPacket
{
    public override void Read(ref PacketReader r) { }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

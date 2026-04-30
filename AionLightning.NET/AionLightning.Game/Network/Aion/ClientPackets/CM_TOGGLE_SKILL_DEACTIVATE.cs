using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client deactivates a toggle skill. Stub — opcode 0xE0.</summary>
public sealed class CM_TOGGLE_SKILL_DEACTIVATE : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadH(); // skillId
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

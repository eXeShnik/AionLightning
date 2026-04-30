using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client selects a bonus title reward. Stub — opcode 0x18B.</summary>
public sealed class CM_BONUS_TITLE : AionClientPacket
{
    public override void Read(ref PacketReader r) => r.ReadH(); // bonusTitleId (0xFFFF = clear)
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

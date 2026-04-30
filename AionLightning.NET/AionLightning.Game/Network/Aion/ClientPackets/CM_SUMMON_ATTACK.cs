using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client orders a summon to attack. Stub — opcode 0x169.</summary>
public sealed class CM_SUMMON_ATTACK : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // summonObjectId
        r.ReadD(); // targetObjectId
        r.ReadC(); // unk
        r.ReadH(); // time
        r.ReadC(); // unk
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

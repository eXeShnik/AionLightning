using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client orders a summon to cast a skill. Stub — opcode 0x16F.</summary>
public sealed class CM_SUMMON_CASTSPELL : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // summonObjId
        r.ReadH(); // skillId
        r.ReadC(); // skillLvl
        r.ReadD(); // targetObjId
        r.ReadF(); // unk
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

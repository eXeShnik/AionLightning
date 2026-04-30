using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client answers a dialog question prompt. Stub — opcode 0xF0.</summary>
public sealed class CM_QUESTION_RESPONSE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // npc objectId
        r.ReadH(); // questionId
        r.ReadC(); // response index
    }

    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client answers a dialog question prompt. Stub — opcode 0xF0.</summary>
public sealed class CM_QUESTION_RESPONSE : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadD(); // questionId
        r.ReadC(); // response (1=yes, 0=no)
        r.ReadC(); // unk
        r.ReadH(); // unk
        r.ReadD(); // sender objectId
        r.ReadD(); // unk
        r.ReadH(); // unk
    }

    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

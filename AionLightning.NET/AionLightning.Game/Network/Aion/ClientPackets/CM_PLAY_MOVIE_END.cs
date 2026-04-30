using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client reports movie/cutscene finished. Stub — opcode 0x113.</summary>
public sealed class CM_PLAY_MOVIE_END : AionClientPacket
{
    public override void Read(ref PacketReader r)
    {
        r.ReadC(); // type
        r.ReadD(); // targetObjectId
        r.ReadD(); // dialogId
        r.ReadH(); // movieId
        r.ReadD(); // unk
    }
    public override ValueTask RunAsync(CancellationToken ct) => ValueTask.CompletedTask;
}

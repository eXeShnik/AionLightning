using AionLightning.Commons.Network;
using QuestEngineType = AionLightning.Game.QuestEngine.QuestEngine;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client reports movie/cutscene finished. Opcode 0x113.</summary>
public sealed class CM_PLAY_MOVIE_END : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly QuestEngineType    _questEngine;

    private int _movieId;

    public CM_PLAY_MOVIE_END(GsClientConnection conn, QuestEngineType questEngine)
    {
        _conn        = conn;
        _questEngine = questEngine;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadC();          // type
        r.ReadD();          // targetObjectId
        r.ReadD();          // dialogId
        _movieId = r.ReadH();
        r.ReadD();          // unk
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        await _questEngine.OnMovieEndAsync(player, _movieId, _conn, ct);
    }
}

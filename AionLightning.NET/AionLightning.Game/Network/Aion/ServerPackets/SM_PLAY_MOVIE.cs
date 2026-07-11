using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Plays a cutscene/movie on the client (Java SM_PLAY_MOVIE port). Opcode 0x69.
/// <paramref name="type"/>: 1 = CutSceneMovies, else CutScenes (Java field comment).
/// </summary>
public sealed class SM_PLAY_MOVIE : AionServerPacket
{
    private readonly int _type;
    private readonly int _movieId;
    private readonly int _id;
    private readonly int _restrictionId;
    private readonly int _objectId;

    public SM_PLAY_MOVIE(int type, int movieId) : base(0x69)
    {
        _type    = type;
        _movieId = movieId;
    }

    public SM_PLAY_MOVIE(int type, int id, int movieId, int restrictionId) : this(type, movieId)
    {
        _id            = id;
        _restrictionId = restrictionId;
    }

    public SM_PLAY_MOVIE(int type, int id, int movieId, int restrictionId, int objectId)
        : this(type, id, movieId, restrictionId)
    {
        _objectId = objectId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC((byte)_type);
        w.WriteD(_objectId);
        w.WriteD(_id);
        w.WriteH(_movieId);
        w.WriteD(_restrictionId);
    }
}

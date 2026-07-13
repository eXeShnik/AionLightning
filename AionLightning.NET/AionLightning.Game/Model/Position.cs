namespace AionLightning.Game.Model;

public readonly record struct Position(float X, float Y, float Z, int Heading, int WorldId, int InstanceId = 0)
{
    public float DistanceTo(Position other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        float dz = Z - other.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// True when two positions share the same broadcast/visibility scope: same world AND same
    /// instance channel. Every "who can see / who to broadcast to" filter must use this instead of
    /// comparing <see cref="WorldId"/> alone, so two channels of the same instanced map stay fully
    /// isolated. Open-world objects all carry <c>InstanceId == 0</c>, so open-world scoping is
    /// unchanged.
    /// </summary>
    public bool SameScope(Position other) => WorldId == other.WorldId && InstanceId == other.InstanceId;
}

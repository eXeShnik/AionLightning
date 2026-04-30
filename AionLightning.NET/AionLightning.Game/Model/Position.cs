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
}

namespace AionLightning.Game.World.Geo;

/// <summary>
/// Geodata query surface (Java GeoService). The real engine is deferred until a
/// .geo dataset is sourced (see migration_plan.md C4 survey) — consumers should
/// call this interface so the engine can slot in behind it later.
/// </summary>
public interface IGeoService
{
    /// <summary>Ground height at (x, y); returns the input z when no geodata is loaded.</summary>
    float GetZ(int worldId, float x, float y, float z);

    /// <summary>Line of sight between two points; always true when no geodata is loaded.</summary>
    bool CanSee(int worldId, float x1, float y1, float z1, float x2, float y2, float z2);

    /// <summary>Clamps a forced-movement destination to the nearest collision; returns the raw target when no geodata is loaded.</summary>
    (float X, float Y, float Z) GetClosestCollision(int worldId, float x, float y, float z, float targetX, float targetY, float targetZ);

    /// <summary>Pure bounds math — needs no geodata (Java world bounds 0..3072 XY, 0..4000 Z).</summary>
    bool IsInBounds(float x, float y, float z);
}

/// <summary>
/// Java DummyGeoData parity — the exact behavior of the Java server with GEO_ENABLE=false,
/// which is how the reference server has always run in this repo (no .geo files exist here).
/// </summary>
public sealed class DummyGeoService : IGeoService
{
    public float GetZ(int worldId, float x, float y, float z) => z;

    public bool CanSee(int worldId, float x1, float y1, float z1, float x2, float y2, float z2) => true;

    public (float X, float Y, float Z) GetClosestCollision(int worldId, float x, float y, float z,
        float targetX, float targetY, float targetZ) => (targetX, targetY, targetZ);

    public bool IsInBounds(float x, float y, float z) =>
        x is >= 0f and <= 3072f && y is >= 0f and <= 3072f && z is >= 0f and <= 4000f;
}

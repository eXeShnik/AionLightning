namespace AionLightning.Game.Model.Siege;

/// <summary>
/// Java model.siege.SiegeShield — a discovered geo-shield volume attached to a fortress. This P1 port
/// keeps only the on/off state referenced by <see cref="SiegeLocation.SetUnderShield"/>; the geometry
/// (Java geoEngine.scene.Spatial bounds) and the zone enter/leave collision handling (Java
/// ShieldService, AttackShieldObserver) depend on the geo/zone engine and are not ported yet — see
/// migration_plan.md. The center/radius fields are kept purely as loaded data for that future wiring.
/// </summary>
public sealed class SiegeShield
{
    public int SiegeLocationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int WorldId { get; init; }
    public float Radius { get; init; }
    public float CenterX { get; init; }
    public float CenterY { get; init; }
    public float CenterZ { get; init; }

    public bool Enabled { get; set; }
}

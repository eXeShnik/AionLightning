namespace AionLightning.Game.Model.Templates.FlyRing;

/// <summary>
/// A flight-training ring from <c>fly_rings.xml</c> (Java <c>model/flyring/FlyRing</c> +
/// <c>FlyRingTemplate</c>). Passing through the ring fires the quest onPassFlyingRing hook. The
/// ring is a disk of <see cref="Radius"/> around <see cref="Center"/>; the p1/p2 points define its
/// plane (kept for future plane-crossing precision — the port currently uses a center-proximity check).
/// </summary>
public sealed record FlyRingTemplate(
    string Name,
    int WorldId,
    float Radius,
    float Cx, float Cy, float Cz);

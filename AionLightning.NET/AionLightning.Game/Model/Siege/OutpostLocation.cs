using AionLightning.Game.Model.Templates.Siege;

namespace AionLightning.Game.Model.Siege;

/// <summary>Java model.siege.OutpostLocation. IsRouteSpawned depended on SiegeService.getFortresses()
/// (cross-location lookup) and is exposed on SiegeService instead of here (SiegeService.IsRouteSpawned),
/// mirroring the ArtifactLocation split — keeps this model free of a service dependency.</summary>
public sealed class OutpostLocation : SiegeLocation
{
    public OutpostLocation(SiegeLocationTemplate template) : base(template)
    {
    }

    public override int GetNextState() => IsVulnerable ? StateInvulnerable : StateVulnerable;

    /// <summary>Java getLocationRace() — @Deprecated hardcoded race-by-id, "should be configured from
    /// datapack". Ported as-is; only the two Balaurea outposts (Elysea/Asmodae underpass) exist.</summary>
    public SiegeRace LocationRace => LocationId switch
    {
        3111 => SiegeRace.ASMODIANS,
        2111 => SiegeRace.ELYOS,
        _ => throw new InvalidOperationException(
            $"OutpostLocation {LocationId} has no hardcoded LocationRace mapping (Java: 'Please move this to datapack')"),
    };

    public IReadOnlyList<int> FortressDependency => Template.FortressDependency;

    public bool IsSiegeAllowed => LocationRace == Race;

    public bool IsSilenteraAllowed => !IsSiegeAllowed && Race != SiegeRace.BALAUR;
}

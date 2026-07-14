using AionLightning.Game.Model.Templates.Siege;

namespace AionLightning.Game.Model.Siege;

/// <summary>Java model.siege.SourceLocation.</summary>
public sealed class SourceLocation : SiegeLocation
{
    public IReadOnlyList<SiegeReward> SiegeRewards { get; }
    public bool IsPreparation { get; set; }

    public SourceLocation(SiegeLocationTemplate template) : base(template)
    {
        SiegeRewards = template.SiegeRewards;
    }

    /// <summary>Java getEntryPosition() — "TODO: move to datapack" hardcoded per-source entry point,
    /// used by clearLocation() (P2) to move enemies out when preparations start. Returns null for any
    /// source id not in the hardcoded table (Java falls through leaving the position unset).</summary>
    public Position? GetEntryPosition() => LocationId switch
    {
        4011 => new Position(332.14316f, 854.36053f, 313.98f, 77, WorldId),
        4021 => new Position(2353.9065f, 378.1945f, 237.8031f, 113, WorldId),
        4031 => new Position(879.23627f, 2712.4644f, 254.25073f, 85, WorldId),
        4041 => new Position(2901.2354f, 2365.0383f, 339.1469f, 39, WorldId),
        _ => null,
    };

    // note: Java's clearLocation()/onEnterZone/onLeaveZone (teleport enemies out, apply the SIEGE
    // zone-type flag while vulnerable) depend on the zone/knownlist/world-move framework — P2.
}

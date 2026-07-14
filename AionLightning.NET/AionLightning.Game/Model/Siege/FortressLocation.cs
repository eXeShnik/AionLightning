using AionLightning.Game.Model.Templates.Siege;

namespace AionLightning.Game.Model.Siege;

/// <summary>Java model.siege.FortressLocation.</summary>
public sealed class FortressLocation : SiegeLocation
{
    public IReadOnlyList<SiegeReward> SiegeRewards { get; }
    public IReadOnlyList<SiegeLegionReward> SiegeLegionRewards { get; }
    public bool IsUnderAssault { get; set; }

    public FortressLocation(SiegeLocationTemplate template) : base(template)
    {
        SiegeRewards = template.SiegeRewards;
        SiegeLegionRewards = template.SiegeLegionRewards;
    }

    public bool IsEnemy(Race playerRace) => SiegeRaceExtensions.FromPlayerRace(playerRace) != Race;

    public override bool CanTeleport(Player? player)
    {
        if (player is null) return CanTeleportFlag;
        return CanTeleportFlag && SiegeRaceExtensions.FromPlayerRace(player.Race) == Race;
    }

    // note: Java's clearLocation()/onEnterZone/onLeaveZone (kick enemies on capture, apply the SIEGE
    // zone-type flag while vulnerable) depend on the zone/knownlist framework — deferred to P2.
}

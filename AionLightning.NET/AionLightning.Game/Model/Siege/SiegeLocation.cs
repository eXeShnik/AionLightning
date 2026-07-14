using AionLightning.Game.Model.Templates.Siege;

namespace AionLightning.Game.Model.Siege;

/// <summary>
/// Base runtime siege-location object (Java model.siege.SiegeLocation). This P1 port keeps the
/// persisted/derived state (ownership, vulnerability, shields, teleport gate) and the getters
/// consumed by the read-only display packets and SiegeService. Java's ZoneHandler plumbing
/// (onEnterZone/onLeaveZone/isInsideLocation/getCreatures/getPlayers/doOnAllPlayers/clearLocation)
/// depended on the zone-instance + knownlist framework and is deferred to P2 alongside the siege
/// zone/collision engine.
/// </summary>
public class SiegeLocation
{
    public const int StateInvulnerable = 0;
    public const int StateVulnerable = 1;

    public SiegeLocationTemplate Template { get; }
    public int LocationId { get; }
    public int WorldId { get; }
    public SiegeType Type { get; }
    public int SiegeDuration { get; }
    public int InfluenceValue { get; }

    public SiegeRace Race { get; set; } = SiegeRace.BALAUR;
    public int LegionId { get; set; }
    public bool IsVulnerable { get; set; }
    public bool IsUnderShield { get; private set; }
    public long LastArtifactActivation { get; set; }

    /// <summary>Java: canTeleport field, gated behind <see cref="CanTeleport"/> (virtual — FortressLocation
    /// additionally requires the querying player's race to match the owning race).</summary>
    protected bool CanTeleportFlag { get; private set; }

    private int _nextState;
    private readonly List<SiegeShield> _shields = new();

    public IReadOnlyList<SiegeShield> Shields => _shields;

    public SiegeLocation(SiegeLocationTemplate template)
    {
        Template = template;
        LocationId = template.Id;
        WorldId = template.World;
        Type = template.Type;
        SiegeDuration = template.SiegeDuration;
        InfluenceValue = template.InfluenceValue;
    }

    /// <summary>Java: 0 invulnerable, 1 vulnerable. Overridden by Outpost (computed from current
    /// vulnerability) and Artifact (always vulnerable).</summary>
    public virtual int GetNextState() => _nextState;

    public void SetNextState(int value) => _nextState = value;

    public void SetShields(IEnumerable<SiegeShield> shields)
    {
        _shields.Clear();
        _shields.AddRange(shields);
    }

    public void SetUnderShield(bool value)
    {
        IsUnderShield = value;
        foreach (var shield in _shields)
            shield.Enabled = value;
    }

    public void SetCanTeleport(bool value) => CanTeleportFlag = value;

    /// <summary>Java isCanTeleport(Player). Base behavior ignores the player; FortressLocation
    /// overrides to additionally require a matching race.</summary>
    public virtual bool CanTeleport(Player? player) => CanTeleportFlag;
}

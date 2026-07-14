using AionLightning.Game.Model.Alliance;

namespace AionLightning.Game.Model.League;

/// <summary>A league seat wrapping one member alliance (Java <c>LeagueMember</c>).</summary>
public sealed class LeagueMember
{
    public PlayerAlliance Alliance { get; }
    public int AllianceId => Alliance.AllianceId;
    public int Position { get; }

    public LeagueMember(PlayerAlliance alliance, int position)
    {
        Alliance = alliance;
        Position = position;
    }
}

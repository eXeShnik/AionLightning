using AionLightning.Game.Model.Alliance;

namespace AionLightning.Game.Model.League;

/// <summary>A league: up to 8 member alliances led by one of them (Java <c>League</c>). In-memory only.</summary>
public sealed class League
{
    public const int MaxMembers = 8;

    private readonly List<LeagueMember> _members = new(MaxMembers);

    public int LeagueId { get; }
    public int LeaderAllianceId { get; private set; }

    // Loot distribution rule — mirrors PlayerAlliance's shape (Java LootGroupRules).
    public int LootDistribution { get; set; }
    public int CommonItemAbove { get; set; }
    public int SuperiorItemAbove { get; set; }
    public int HeroicItemAbove { get; set; }
    public int FabledItemAbove { get; set; }
    public int EthernalItemAbove { get; set; }
    public int AutoDistribution { get; set; }

    public IReadOnlyList<LeagueMember> Members => _members;
    public IEnumerable<LeagueMember> SortedMembers => _members.OrderBy(m => m.Position);
    public bool IsFull => _members.Count >= MaxMembers;

    public League(int leagueId, PlayerAlliance leaderAlliance)
    {
        LeagueId = leagueId;
        LeaderAllianceId = leaderAlliance.AllianceId;
        _members.Add(new LeagueMember(leaderAlliance, 0));
    }

    public bool HasMember(int allianceId) => _members.Any(m => m.AllianceId == allianceId);
    public LeagueMember? GetMember(int allianceId) => _members.FirstOrDefault(m => m.AllianceId == allianceId);

    public bool AddAlliance(PlayerAlliance alliance)
    {
        if (IsFull || HasMember(alliance.AllianceId)) return false;
        _members.Add(new LeagueMember(alliance, _members.Count));
        return true;
    }

    /// <summary>Removes a member alliance; auto-promotes the first remaining alliance when the leader leaves.</summary>
    public bool RemoveAlliance(int allianceId)
    {
        var member = GetMember(allianceId);
        if (member is null) return false;
        _members.Remove(member);

        if (LeaderAllianceId == allianceId)
        {
            var next = _members.FirstOrDefault();
            if (next is not null)
                LeaderAllianceId = next.AllianceId;
        }

        return true;
    }

    public bool IsLeader(int allianceId) => allianceId == LeaderAllianceId;
}

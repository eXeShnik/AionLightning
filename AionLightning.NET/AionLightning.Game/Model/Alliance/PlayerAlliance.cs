namespace AionLightning.Game.Model.Alliance;

/// <summary>
/// A raid alliance: up to 4 subgroups of 6 players (24 total) plus a captain, up to 4 vice-captains,
/// and an optional league membership (Java <c>PlayerAlliance</c>). In-memory only, mirroring
/// <c>PlayerGroup</c>'s lifecycle — nothing here is persisted.
/// </summary>
public sealed class PlayerAlliance
{
    public const int MaxMembers = 24;
    public const int MaxViceCaptains = 4;
    private const int FirstGroupId = 1000;
    private const int SubGroupCount = 4;

    private readonly Dictionary<int, PlayerAllianceGroup> _groups = new(SubGroupCount);

    public int AllianceId { get; }
    public int CaptainObjectId { get; private set; }
    public List<int> ViceCaptainIds { get; } = new();

    /// <summary>Non-null once this alliance has joined a league (Java <c>PlayerAlliance.league</c>).</summary>
    public League.League? League { get; set; }

    /// <summary>Countdown used by the ready-check command sequence (Java <c>allianceReadyStatus</c>).</summary>
    public int ReadyStatus { get; set; }

    // Loot distribution rule — same field shape as PlayerGroup so CM_DISTRIBUTION_SETTINGS can drive both.
    public int LootDistribution { get; set; }
    public int LootMisc { get; set; }
    public int CommonItemAbove { get; set; }
    public int SuperiorItemAbove { get; set; }
    public int HeroicItemAbove { get; set; }
    public int FabledItemAbove { get; set; }
    public int EthernalItemAbove { get; set; }
    public int AutoDistribution { get; set; }

    public IReadOnlyCollection<PlayerAllianceGroup> Groups => _groups.Values;
    public IEnumerable<PlayerAllianceMember> Members => _groups.Values.SelectMany(g => g.Members);
    public int MemberCount => _groups.Values.Sum(g => g.Members.Count);
    public bool IsFull => MemberCount >= MaxMembers;
    public bool IsInLeague => League is not null;

    public PlayerAlliance(int allianceId, Player captain)
    {
        AllianceId = allianceId;
        CaptainObjectId = captain.ObjectId;
        for (int i = 0; i < SubGroupCount; i++)
            _groups[FirstGroupId + i] = new PlayerAllianceGroup(FirstGroupId + i);
    }

    public PlayerAllianceGroup? GetGroup(int groupId) => _groups.TryGetValue(groupId, out var g) ? g : null;

    public PlayerAllianceGroup? GetOpenGroup() => _groups.Values.FirstOrDefault(g => !g.IsFull);

    public PlayerAllianceMember? GetMember(int objectId) => Members.FirstOrDefault(m => m.ObjectId == objectId);

    public bool HasMember(int objectId) => Members.Any(m => m.ObjectId == objectId);

    /// <summary>Adds to the given subgroup, or the first open one when <paramref name="groupId"/> is null.</summary>
    public bool AddMember(PlayerAllianceMember member, int? groupId = null)
    {
        var group = groupId is int gid ? GetGroup(gid) : GetOpenGroup();
        return group is not null && group.AddMember(member);
    }

    /// <summary>
    /// Removes a member from its subgroup. When the departing member was the captain, auto-promotes
    /// the next vice-captain still present, else the first remaining member (Java
    /// <c>ChangeAllianceLeaderEvent</c>'s auto-succession path, folded directly into removal here to
    /// mirror how <c>PlayerGroup.RemoveMember</c> handles the same case).
    /// </summary>
    public bool RemoveMember(int objectId)
    {
        var group = _groups.Values.FirstOrDefault(g => g.Members.Any(m => m.ObjectId == objectId));
        if (group is null || !group.RemoveMember(objectId)) return false;

        ViceCaptainIds.Remove(objectId);

        if (CaptainObjectId == objectId)
        {
            int nextCaptain = ViceCaptainIds.FirstOrDefault(HasMember);
            if (nextCaptain != 0)
            {
                CaptainObjectId = nextCaptain;
                ViceCaptainIds.Remove(nextCaptain);
            }
            else
            {
                var firstMember = Members.FirstOrDefault();
                if (firstMember is not null)
                    CaptainObjectId = firstMember.ObjectId;
            }
        }

        return true;
    }

    public bool IsLeader(int objectId) => objectId == CaptainObjectId;
    public bool IsViceCaptain(int objectId) => ViceCaptainIds.Contains(objectId);
    public bool IsSomeCaptain(int objectId) => IsLeader(objectId) || IsViceCaptain(objectId);

    /// <summary>Explicit captain reassignment (ALLIANCE_SET_CAPTAIN); does not touch the vice-captain list.</summary>
    public void SetLeader(int objectId)
    {
        if (HasMember(objectId))
            CaptainObjectId = objectId;
    }
}

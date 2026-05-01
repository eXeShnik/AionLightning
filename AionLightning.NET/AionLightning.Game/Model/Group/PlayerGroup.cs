using AionLightning.Game.Model;

namespace AionLightning.Game.Model.Group;

public sealed class PlayerGroup
{
    public const int MaxMembers = 6;

    private readonly List<Player> _members;

    public int GroupId { get; }
    public int LeaderObjectId { get; private set; }

    // Loot distribution rule (0=free-for-all, 1=round-robin, 2=leader)
    public int LootDistribution { get; set; }
    public int LootMisc { get; set; }
    // Quality thresholds above which the auto-distribution rule applies (0=all items)
    public int CommonItemAbove     { get; set; }
    public int SuperiorItemAbove   { get; set; }
    public int HeroicItemAbove     { get; set; }
    public int FabledItemAbove     { get; set; }
    public int EthernalItemAbove   { get; set; }
    // Auto-distribution mode (0=normal, 2=roll dice, 3=bid)
    public int AutoDistribution { get; set; }

    public IReadOnlyList<Player> Members => _members;
    public bool IsFull => _members.Count >= MaxMembers;
    public bool IsEmpty => _members.Count == 0;

    public PlayerGroup(int groupId, Player leader)
    {
        GroupId          = groupId;
        LeaderObjectId   = leader.ObjectId;
        _members         = new List<Player>(MaxMembers) { leader };
    }

    public bool AddMember(Player player)
    {
        if (IsFull) return false;
        _members.Add(player);
        return true;
    }

    public bool RemoveMember(int objectId)
    {
        var m = _members.FirstOrDefault(p => p.ObjectId == objectId);
        if (m is null) return false;
        _members.Remove(m);
        if (_members.Count > 0 && LeaderObjectId == objectId)
            LeaderObjectId = _members[0].ObjectId;
        return true;
    }

    public bool IsLeader(int objectId) => objectId == LeaderObjectId;
    public bool HasMember(int objectId) => _members.Any(p => p.ObjectId == objectId);

    public void SetLeader(int objectId)
    {
        if (HasMember(objectId))
            LeaderObjectId = objectId;
    }
}

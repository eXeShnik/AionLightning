namespace AionLightning.Game.Model.Alliance;

/// <summary>One of an alliance's four 6-player subgroups (Java <c>PlayerAllianceGroup</c>).</summary>
public sealed class PlayerAllianceGroup
{
    public const int MaxMembers = 6;

    private readonly List<PlayerAllianceMember> _members = new(MaxMembers);

    public int GroupId { get; }
    public IReadOnlyList<PlayerAllianceMember> Members => _members;
    public bool IsFull => _members.Count >= MaxMembers;
    public bool IsEmpty => _members.Count == 0;

    public PlayerAllianceGroup(int groupId) => GroupId = groupId;

    public bool AddMember(PlayerAllianceMember member)
    {
        if (IsFull) return false;
        _members.Add(member);
        member.AllianceGroupId = GroupId;
        return true;
    }

    public bool RemoveMember(int objectId)
    {
        var m = _members.FirstOrDefault(x => x.ObjectId == objectId);
        if (m is null) return false;
        _members.Remove(m);
        return true;
    }
}

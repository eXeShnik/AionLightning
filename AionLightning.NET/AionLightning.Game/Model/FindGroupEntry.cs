namespace AionLightning.Game.Model;

/// <summary>
/// One entry in the Find Group (LFG) board — either a recruiter (group looking for members)
/// or an applicant (player looking for a group).
/// </summary>
public sealed class FindGroupEntry
{
    public int    ObjectId   { get; }
    public string Name       { get; }
    public string Message    { get; set; }
    public byte   GroupType  { get; }   // 0=group, 1=alliance
    public byte   MemberSize { get; }
    public byte   MinLevel   { get; }
    public byte   MaxLevel   { get; }
    public byte   ClassId    { get; }   // for applicant board
    public bool   IsPlayer   { get; }   // true = solo player, unk field = 65557
    public int    LastUpdate { get; private set; }

    public FindGroupEntry(int objectId, string name, string message, byte groupType,
        byte memberSize, byte minLevel, byte maxLevel, byte classId, bool isPlayer)
    {
        ObjectId   = objectId;
        Name       = name;
        Message    = message;
        GroupType  = groupType;
        MemberSize = memberSize;
        MinLevel   = minLevel;
        MaxLevel   = maxLevel;
        ClassId    = classId;
        IsPlayer   = isPlayer;
        LastUpdate = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    public void UpdateMessage(string message)
    {
        Message    = message;
        LastUpdate = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
}

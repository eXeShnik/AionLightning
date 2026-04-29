namespace AionLightning.Game.Model.Skill;

public sealed class PlayerSkillEntry
{
    public int  SkillId    { get; }
    public int  SkillLevel { get; private set; }
    public bool IsStigma   { get; }

    public PlayerSkillEntry(int skillId, int skillLevel, bool isStigma = false)
    {
        SkillId    = skillId;
        SkillLevel = skillLevel;
        IsStigma   = isStigma;
    }

    public void SetLevel(int level) => SkillLevel = level;
}

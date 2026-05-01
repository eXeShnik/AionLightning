namespace AionLightning.Game.Model.Skill;

public sealed class PlayerSkillEntry
{
    public int      SkillId    { get; }
    public int      SkillLevel { get; private set; }
    public bool     IsStigma   { get; }
    public DateTime LastUsedAt { get; private set; } = DateTime.MinValue;

    public PlayerSkillEntry(int skillId, int skillLevel, bool isStigma = false)
    {
        SkillId    = skillId;
        SkillLevel = skillLevel;
        IsStigma   = isStigma;
    }

    public void SetLevel(int level) => SkillLevel = level;
    public void MarkUsed()          => LastUsedAt = DateTime.UtcNow;

    public bool IsOnCooldown(int cooldownMs)
        => cooldownMs > 0 && (DateTime.UtcNow - LastUsedAt).TotalMilliseconds < cooldownMs;
}

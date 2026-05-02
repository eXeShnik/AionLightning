namespace AionLightning.Game.Model.Skill;

public sealed class PlayerSkillList
{
    private readonly Dictionary<int, PlayerSkillEntry> _basic  = new();
    private readonly Dictionary<int, PlayerSkillEntry> _stigma = new();

    public IEnumerable<PlayerSkillEntry> BasicSkills  => _basic.Values;
    public IEnumerable<PlayerSkillEntry> StigmaSkills => _stigma.Values;
    public IEnumerable<PlayerSkillEntry> AllSkills    => _basic.Values.Concat(_stigma.Values);

    public bool AddSkill(int skillId, int skillLevel, bool isStigma = false)
    {
        var dict = isStigma ? _stigma : _basic;
        if (dict.TryGetValue(skillId, out var existing))
        {
            if (existing.SkillLevel >= skillLevel) return false;
            existing.SetLevel(skillLevel);
            return true;
        }
        dict[skillId] = new PlayerSkillEntry(skillId, skillLevel, isStigma);
        return true;
    }

    public bool RemoveStigmaSkill(int skillId) => _stigma.Remove(skillId);
    public bool IsPresent(int skillId) => _basic.ContainsKey(skillId) || _stigma.ContainsKey(skillId);
    public int  GetLevel(int skillId)  => _basic.TryGetValue(skillId, out var e) ? e.SkillLevel :
                                          _stigma.TryGetValue(skillId, out e)   ? e.SkillLevel : 0;
    public PlayerSkillEntry? GetEntry(int skillId) => _basic.TryGetValue(skillId, out var e) ? e :
                                                       _stigma.TryGetValue(skillId, out e)   ? e : null;
    public int  Count => _basic.Count + _stigma.Count;
}

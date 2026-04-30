using System.Xml;
using System.Xml.Serialization;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class SkillTreeData
{
    private readonly Dictionary<int, List<SkillLearnTemplate>> _byHash    = new();
    private readonly Dictionary<int, List<SkillLearnTemplate>> _bySkillId = new();

    public void Load(string dataRoot, ILogger log)
    {
        var file = Path.Combine(dataRoot, "skill_tree", "skill_tree.xml");
        if (!File.Exists(file))
        {
            log.LogWarning("SkillTreeData: file not found at {File}", file);
            return;
        }

        var serializer = new XmlSerializer(typeof(SkillTreeXml));
        using var reader = XmlReader.Create(file);
        var root = (SkillTreeXml?)serializer.Deserialize(reader);
        if (root?.Skills is null) return;

        foreach (var t in root.Skills)
        {
            int hash = MakeHash((int)t.ClassId, (int)t.Race, t.MinLevel);
            if (!_byHash.TryGetValue(hash, out var list))
                _byHash[hash] = list = new List<SkillLearnTemplate>();
            list.Add(t);

            if (!_bySkillId.TryGetValue(t.SkillId, out var idList))
                _bySkillId[t.SkillId] = idList = new List<SkillLearnTemplate>();
            idList.Add(t);
        }

        log.LogInformation("SkillTreeData: loaded {Count} skill learn entries", root.Skills.Count);
    }

    // Returns skills that become available at exactly `level` for this class+race combination.
    // Checks class+race, class+PC_ALL, and ALL+PC_ALL buckets (mirrors Java logic).
    public IEnumerable<SkillLearnTemplate> GetTemplatesFor(PlayerClass cls, int level, Race race)
    {
        var results = new List<SkillLearnTemplate>();
        TryAdd(results, MakeHash((int)cls,             (int)race,        level));
        TryAdd(results, MakeHash((int)cls,             (int)Race.PC_ALL, level));
        TryAdd(results, MakeHash((int)PlayerClass.ALL, (int)Race.PC_ALL, level));
        return results;
    }

    /// <summary>
    /// Returns the highest skill level available for this skillId at the player's current level+class+race.
    /// Used when learning skills from skill books. Returns 1 if no tree entry exists.
    /// </summary>
    public int GetMaxSkillLevel(int skillId, PlayerClass cls, Race race, int playerLevel)
    {
        if (!_bySkillId.TryGetValue(skillId, out var candidates))
            return 1;

        int maxLevel = 0;
        foreach (var t in candidates)
        {
            if (t.MinLevel > playerLevel) continue;
            bool matchClass = t.ClassId == PlayerClass.ALL || t.ClassId == cls;
            bool matchRace  = t.Race    == Race.PC_ALL     || t.Race    == race;
            if (matchClass && matchRace && t.SkillLevel > maxLevel)
                maxLevel = t.SkillLevel;
        }
        return maxLevel > 0 ? maxLevel : 1;
    }

    private void TryAdd(List<SkillLearnTemplate> results, int hash)
    {
        if (_byHash.TryGetValue(hash, out var list)) results.AddRange(list);
    }

    private static int MakeHash(int classId, int race, int level)
        => ((classId << 8 | race) << 8) | level;
}

[XmlRoot("skill_tree")]
public sealed class SkillTreeXml
{
    [XmlElement("skill")] public List<SkillLearnTemplate> Skills { get; set; } = new();
}

using System.Xml;
using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Skill;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class SkillData
{
    private readonly Dictionary<int, SkillTemplate> _templates = new();
    private Dictionary<int, List<int>>? _cooldownGroups;

    public void Load(string dataRoot, ILogger log)
    {
        var file = Path.Combine(dataRoot, "skills", "skill_templates.xml");
        if (!File.Exists(file))
        {
            log.LogWarning("SkillData: file not found at {File}", file);
            return;
        }

        var serializer = new XmlSerializer(typeof(SkillDataXml));
        using var reader = XmlReader.Create(file);
        var root = (SkillDataXml?)serializer.Deserialize(reader);

        if (root?.Skills is null) return;

        foreach (var t in root.Skills)
            _templates[t.SkillId] = t;

        log.LogInformation("SkillData: loaded {Count} skill templates", _templates.Count);
    }

    public SkillTemplate? GetTemplate(int skillId)
        => _templates.TryGetValue(skillId, out var t) ? t : null;

    public int Size => _templates.Count;

    public IReadOnlyList<int> GetSkillsForCooldownId(int cooldownId)
    {
        _cooldownGroups ??= BuildCooldownGroups();
        return _cooldownGroups.TryGetValue(cooldownId, out var list) ? list : [];
    }

    private Dictionary<int, List<int>> BuildCooldownGroups()
    {
        var groups = new Dictionary<int, List<int>>();
        foreach (var t in _templates.Values)
        {
            if (!groups.TryGetValue(t.CooldownId, out var list))
                groups[t.CooldownId] = list = new List<int>();
            list.Add(t.SkillId);
        }
        return groups;
    }
}

[XmlRoot("skill_data")]
public sealed class SkillDataXml
{
    [XmlElement("skill_template")] public List<SkillTemplate> Skills { get; set; } = new();
}

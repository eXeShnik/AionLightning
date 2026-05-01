using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class NpcSkillData
{
    public sealed record NpcSkillEntry(int SkillId, int SkillLevel, int Probability);

    private readonly Dictionary<int, List<NpcSkillEntry>> _skills = new();

    public void Load(string dataRoot, ILogger log)
    {
        var file = Path.Combine(dataRoot, "npc_skills.xml");
        if (!File.Exists(file))
        {
            log.LogWarning("NpcSkillData: npc_skills.xml not found at {File}", file);
            return;
        }
        int npcCount = LoadFile(file, log);
        log.LogInformation("NpcSkillData: loaded skill sets for {NpcCount} NPCs", npcCount);
    }

    private int LoadFile(string path, ILogger log)
    {
        int loaded = 0;
        try
        {
            using var reader = XmlReader.Create(path, new XmlReaderSettings
            {
                IgnoreComments   = true,
                IgnoreWhitespace = true
            });

            int currentNpcId = 0;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element when reader.LocalName == "npcskills":
                        if (int.TryParse(reader.GetAttribute("npcid"), out int npcId))
                        {
                            currentNpcId = npcId;
                            if (!_skills.ContainsKey(npcId))
                                _skills[npcId] = new List<NpcSkillEntry>();
                        }
                        break;

                    case XmlNodeType.Element when reader.LocalName == "npcskill" && currentNpcId != 0:
                        if (int.TryParse(reader.GetAttribute("skillid"), out int skillId)
                            && int.TryParse(reader.GetAttribute("skilllevel"), out int skillLevel))
                        {
                            int.TryParse(reader.GetAttribute("probability"), out int prob);
                            _skills[currentNpcId].Add(new NpcSkillEntry(skillId, skillLevel, Math.Clamp(prob, 1, 100)));
                        }
                        break;

                    case XmlNodeType.EndElement when reader.LocalName == "npcskills":
                        if (currentNpcId != 0)
                        {
                            loaded++;
                            currentNpcId = 0;
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "NpcSkillData: failed to parse {File}", path);
        }
        return loaded;
    }

    public IReadOnlyList<NpcSkillEntry>? GetSkills(int npcId)
        => _skills.TryGetValue(npcId, out var list) ? list : null;
}

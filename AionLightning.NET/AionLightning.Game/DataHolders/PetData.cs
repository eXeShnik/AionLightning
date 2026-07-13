using System.Globalization;
using System.Xml;
using AionLightning.Game.Model.Templates.Pet;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class PetData
{
    private readonly Dictionary<int, PetTemplate> _templates = new();

    public int Count => _templates.Count;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "pets", "pets.xml");
        if (!File.Exists(path)) { log.LogWarning("PetData: pets.xml not found at {P}", path); return; }

        using var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });

        int petId = 0;
        string name = string.Empty;
        int nameId = 0;
        int conditionReward = 0;
        List<PetFunction>? functions = null;
        PetStatsTemplate? stats = null;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "pet")
            {
                petId = int.TryParse(reader.GetAttribute("id"), out int id) ? id : 0;
                name = reader.GetAttribute("name") ?? string.Empty;
                nameId = int.TryParse(reader.GetAttribute("nameid"), out int nid) ? nid : 0;
                conditionReward = int.TryParse(reader.GetAttribute("condition_reward"), out int cr) ? cr : 0;
                functions = new List<PetFunction>();
                stats = null;
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "petfunction" && functions is not null)
            {
                functions.Add(new PetFunction(
                    Type: reader.GetAttribute("type") ?? string.Empty,
                    Id: int.TryParse(reader.GetAttribute("id"), out int fid) ? fid : 0,
                    Slots: int.TryParse(reader.GetAttribute("slots"), out int slots) ? slots : 0));
                continue;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "petstats")
            {
                stats = new PetStatsTemplate(
                    Reaction: reader.GetAttribute("reaction") ?? string.Empty,
                    RunSpeed: float.TryParse(reader.GetAttribute("run_speed"), NumberStyles.Float, CultureInfo.InvariantCulture, out float rs) ? rs : 0f,
                    WalkSpeed: float.TryParse(reader.GetAttribute("walk_speed"), NumberStyles.Float, CultureInfo.InvariantCulture, out float ws) ? ws : 0f,
                    Height: float.TryParse(reader.GetAttribute("height"), NumberStyles.Float, CultureInfo.InvariantCulture, out float h) ? h : 0f,
                    Altitude: float.TryParse(reader.GetAttribute("altitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out float alt) ? alt : 0f);
                continue;
            }

            if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "pet" && petId != 0)
            {
                _templates[petId] = new PetTemplate(petId, name, nameId, conditionReward, functions ?? new List<PetFunction>(), stats);
                petId = 0;
            }
        }

        log.LogInformation("PetData: loaded {Count} pet templates", _templates.Count);
    }

    public PetTemplate? GetTemplate(int petId) => _templates.GetValueOrDefault(petId);
}

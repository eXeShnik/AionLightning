using System.Xml;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class DropData
{
    // npcId → ordered list of drop groups
    private readonly Dictionary<int, List<NpcDropGroup>> _npcDrops = new();

    public sealed class NpcDropGroup
    {
        public string            Name    { get; set; } = string.Empty;
        public List<NpcDropEntry> Entries { get; set; } = new();
    }

    public sealed class NpcDropEntry
    {
        public int   ItemId    { get; set; }
        public float Chance    { get; set; }
        public int   MinAmount { get; set; }
        public int   MaxAmount { get; set; }
    }

    public void Load(string dataRoot, ILogger log)
    {
        var dir = Path.Combine(dataRoot, "npc_drops");
        if (!Directory.Exists(dir))
        {
            log.LogWarning("DropData: npc_drops directory not found at {Dir}", dir);
            return;
        }

        int fileCount = 0;
        int npcCount  = 0;
        foreach (var file in Directory.EnumerateFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
        {
            npcCount += LoadFile(file, log);
            fileCount++;
        }

        log.LogInformation("DropData: loaded {NpcCount} NPC drop tables from {FileCount} files", npcCount, fileCount);
    }

    private int LoadFile(string path, ILogger log)
    {
        int loaded = 0;
        try
        {
            using var reader = XmlReader.Create(path, new XmlReaderSettings
            {
                IgnoreComments  = true,
                IgnoreWhitespace = true
            });

            int           currentNpcId = 0;
            NpcDropGroup? currentGroup = null;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element when reader.LocalName == "npc_drop":
                        if (int.TryParse(reader.GetAttribute("npc_id"), out int npcId))
                        {
                            currentNpcId = npcId;
                            if (!_npcDrops.ContainsKey(npcId))
                                _npcDrops[npcId] = new List<NpcDropGroup>();
                        }
                        break;

                    case XmlNodeType.Element when reader.LocalName == "drop_group" && currentNpcId != 0:
                        currentGroup = new NpcDropGroup { Name = reader.GetAttribute("name") ?? string.Empty };
                        _npcDrops[currentNpcId].Add(currentGroup);
                        break;

                    case XmlNodeType.Element when reader.LocalName == "drop" && currentGroup is not null:
                        if (int.TryParse(reader.GetAttribute("item_id"), out int itemId)
                            && float.TryParse(reader.GetAttribute("chance"),
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out float chance))
                        {
                            int.TryParse(reader.GetAttribute("min_amount"), out int minAmt);
                            int.TryParse(reader.GetAttribute("max_amount"), out int maxAmt);
                            minAmt = Math.Max(1, minAmt);
                            maxAmt = Math.Max(minAmt, maxAmt);

                            currentGroup.Entries.Add(new NpcDropEntry
                            {
                                ItemId    = itemId,
                                Chance    = Math.Clamp(chance, 0f, 100f),
                                MinAmount = minAmt,
                                MaxAmount = maxAmt,
                            });
                        }
                        break;

                    case XmlNodeType.EndElement when reader.LocalName == "drop_group":
                        currentGroup = null;
                        break;

                    case XmlNodeType.EndElement when reader.LocalName == "npc_drop":
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
            log.LogWarning(ex, "DropData: failed to parse {File}", path);
        }
        return loaded;
    }

    public IReadOnlyList<NpcDropGroup>? GetDropGroups(int npcId)
        => _npcDrops.TryGetValue(npcId, out var groups) ? groups : null;

    public bool HasDrops(int npcId) => _npcDrops.ContainsKey(npcId);
}

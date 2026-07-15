using System.Xml.Serialization;
using AionLightning.Game.Model.Templates.Challenge;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

/// <summary>
/// Port of Java dataholders.ChallengeData — loads data/static_data/quest_data/challenge_tasks.xml into
/// the legion/town challenge-task template map consumed by <see cref="Services.ChallengeTaskService"/>.
/// </summary>
public sealed class ChallengeData
{
    private readonly Dictionary<int, ChallengeTaskTemplate> _tasksById = new();

    public IReadOnlyDictionary<int, ChallengeTaskTemplate> Tasks => _tasksById;
    public int Count => _tasksById.Count;

    public void Load(string dataRoot, ILogger log)
    {
        var path = Path.Combine(dataRoot, "quest_data", "challenge_tasks.xml");
        if (!File.Exists(path))
        {
            log.LogWarning("ChallengeData: file not found: {Path}", path);
            return;
        }

        var serializer = new XmlSerializer(typeof(ChallengeTasksXml));
        using var fs   = File.OpenRead(path);
        var root       = (ChallengeTasksXml?)serializer.Deserialize(fs);
        if (root is null) return;

        foreach (var t in root.Tasks)
            _tasksById[t.Id] = t;

        log.LogInformation("ChallengeData: loaded {Count} challenge task templates", _tasksById.Count);
    }

    public ChallengeTaskTemplate? GetTaskByTaskId(int taskId) => _tasksById.GetValueOrDefault(taskId);

    /// <summary>Java ChallengeData.getTaskByQuestId — the task owning the given challenge quest id, or
    /// null if that quest doesn't belong to any known challenge task (the cheap guard
    /// <see cref="Services.ChallengeTaskService.OnChallengeQuestFinishAsync"/> relies on).</summary>
    public ChallengeTaskTemplate? GetTaskByQuestId(int questId) =>
        _tasksById.Values.FirstOrDefault(t => t.Quests.Any(q => q.Id == questId));

    public ChallengeQuestTemplate? GetQuestByQuestId(int questId) =>
        _tasksById.Values.SelectMany(t => t.Quests).FirstOrDefault(q => q.Id == questId);

    [XmlRoot("challenge_tasks")]
    public sealed class ChallengeTasksXml
    {
        [XmlElement("task")]
        public List<ChallengeTaskTemplate> Tasks { get; set; } = new();
    }
}

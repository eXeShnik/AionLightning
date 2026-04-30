namespace AionLightning.Game.Model.Quest;

public sealed class PlayerQuestList
{
    private readonly Dictionary<int, QuestEntry> _quests = new();

    public void Add(QuestEntry entry)     => _quests[entry.QuestId] = entry;
    public void Remove(int questId)       => _quests.Remove(questId);
    public QuestEntry? Get(int questId)   => _quests.GetValueOrDefault(questId);
    public bool Contains(int questId)     => _quests.ContainsKey(questId);

    public IEnumerable<QuestEntry> Active
        => _quests.Values.Where(q => q.Status is QuestStatus.START or QuestStatus.REWARD);

    public IEnumerable<QuestEntry> Completed
        => _quests.Values.Where(q => q.Status == QuestStatus.COMPLETE);
}

using AionLightning.Game.Model.Quest;

namespace AionLightning.Game.Dao;

public interface IQuestDao
{
    Task<IReadOnlyList<QuestEntry>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default);
    Task UpsertAsync(int playerId, QuestEntry entry, CancellationToken ct = default);
    Task SaveAllAsync(int playerId, IEnumerable<QuestEntry> quests, CancellationToken ct = default);
    Task DeleteAsync(int playerId, int questId, CancellationToken ct = default);
}

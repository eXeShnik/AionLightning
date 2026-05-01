namespace AionLightning.Game.Dao;

public interface ISkillDao
{
    Task<IReadOnlyList<(int SkillId, int SkillLevel)>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default);
    Task UpsertAsync(int playerId, int skillId, int skillLevel, CancellationToken ct = default);
}

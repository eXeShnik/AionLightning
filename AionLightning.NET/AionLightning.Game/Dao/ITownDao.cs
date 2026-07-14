using AionLightning.Game.Model;

namespace AionLightning.Game.Dao;

public sealed record TownRow(int Id, int Level, int Points, DateTime LevelUpDate);

/// <summary>Java dao.TownDAO — persists per-district level/points state (the `towns` table).</summary>
public interface ITownDao
{
    Task<List<TownRow>> LoadAsync(Race race, CancellationToken ct = default);
    Task UpsertAsync(int id, int level, int points, Race race, DateTime levelUpDate, CancellationToken ct = default);
}

using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Dao;

public sealed record SiegeLocationRow(int Id, SiegeRace Race, int LegionId);

/// <summary>Java dao.SiegeDAO — persists the `siege_locations` runtime table (id, race, legion_id).</summary>
public interface ISiegeDao
{
    Task<List<SiegeLocationRow>> LoadAllAsync(CancellationToken ct = default);
    Task UpsertAsync(int locationId, SiegeRace race, int legionId, CancellationToken ct = default);
}

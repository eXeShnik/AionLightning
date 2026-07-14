using AionLightning.Game.Model.Siege;

namespace AionLightning.Game.Dao;

public sealed record BaseRow(int Id, int MapId, SiegeRace Race);

/// <summary>Java dao.BaseDAO — persists the `bases` runtime table (id, map_id, race), the .NET
/// counterpart of Java's `` `base`(mapid, id, race, last_time) `` table.</summary>
public interface IBaseDao
{
    Task<List<BaseRow>> LoadAllAsync(CancellationToken ct = default);
    Task UpsertAsync(int id, int mapId, SiegeRace race, CancellationToken ct = default);
}

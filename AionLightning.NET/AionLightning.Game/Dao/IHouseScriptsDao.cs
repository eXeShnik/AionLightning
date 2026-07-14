namespace AionLightning.Game.Dao;

/// <summary>
/// Port of Java dao.HouseScriptsDAO — per-house, per-slot decoration script XML persisted as plaintext
/// (compression/decompression only ever happens in memory, see PlayerScripts/HouseScriptCompression).
/// </summary>
public interface IHouseScriptsDao
{
    /// <summary>Java HouseScriptsDAO.getPlayerScripts(int) minus the PlayerScripts construction — raw
    /// (position, script) rows for the house; the caller (HousingService) applies them onto an
    /// already-created PlayerScripts container.</summary>
    Task<List<(int Position, string? Script)>> LoadAsync(int houseId, CancellationToken ct = default);

    Task AddScriptAsync(int houseId, int position, string? script, CancellationToken ct = default);

    Task UpdateScriptAsync(int houseId, int position, string? script, CancellationToken ct = default);

    Task DeleteScriptAsync(int houseId, int position, CancellationToken ct = default);
}

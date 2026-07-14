using System.Collections.Concurrent;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Town;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Java services.TownService — tracks each town's (Sanctum/Pandaemonium district) persisted level,
/// consulted by the housing owner-info packet and (once a TownSpawnsData equivalent exists) NPC
/// availability. On first boot with an empty `towns` table, seeds one row per distinct town_id found
/// in houses.xml addresses (Java's own bootstrap import), inferring race from the housing land's
/// manager NPC tribe (GENERAL == Elyos, same convention Java used).
/// note: Java's SM_TOWNS_LIST send/level-up NPC respawn both depend on subsystems not carried over in
/// full here — see <see cref="Model.Town.Town"/>'s doc comment.
/// </summary>
public sealed class TownService
{
    private readonly ConcurrentDictionary<int, Town> _towns = new();
    private readonly ITownDao _townDao;
    private readonly IDataManager _dataManager;
    private readonly ILogger<TownService> _log;

    public TownService(ITownDao townDao, IDataManager dataManager, ILogger<TownService> log)
    {
        _townDao = townDao;
        _dataManager = dataManager;
        _log = log;
    }

    /// <summary>Java TownService() constructor — loads persisted towns, or seeds them from houses.xml
    /// on an empty table. Called once at startup by <see cref="TownServiceHostedService"/>.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        foreach (var race in new[] { Race.ELYOS, Race.ASMODIANS })
        {
            var rows = await _townDao.LoadAsync(race, ct);
            foreach (var row in rows)
                _towns[row.Id] = new Town(row.Id, row.Level, row.Points, race, row.LevelUpDate);
        }

        if (_towns.IsEmpty)
        {
            foreach (var land in _dataManager.Housing.Lands)
            {
                foreach (var address in land.Addresses)
                {
                    if (address.TownId == 0 || _towns.ContainsKey(address.TownId)) continue;

                    var managerTribe = _dataManager.Npcs.GetTemplate(land.ManagerNpcId)?.Tribe;
                    var race = string.Equals(managerTribe, "GENERAL", StringComparison.OrdinalIgnoreCase)
                        ? Race.ELYOS : Race.ASMODIANS;

                    var town = Town.CreateNew(address.TownId, race);
                    _towns[town.Id] = town;
                    await _townDao.UpsertAsync(town.Id, town.Level, town.Points, town.Race, town.LevelUpDate, ct);
                }
            }
        }

        _log.LogInformation("TownService: loaded {Count} towns", _towns.Count);
    }

    public Town? GetTownById(int townId) => _towns.GetValueOrDefault(townId);

    /// <summary>Java TownService's per-race town maps (elyosTowns/asmosTowns), exposed for the
    /// SM_TOWNS_LIST login broadcast (Java TownService.onEnterWorld).</summary>
    public IReadOnlyCollection<Town> GetTownsForRace(Race race) =>
        _towns.Values.Where(t => t.Race == race).ToList();

    /// <summary>Java TownService.getTownById(id).getLevel() — 1 (base level) for an unknown/zero town id,
    /// matching the housing owner-info packet's pre-TownService stub default.</summary>
    public int GetTownLevel(int townId) => townId != 0 && _towns.TryGetValue(townId, out var town) ? town.Level : 1;

    /// <summary>Java TownService.getTownResidence(Player) — the town id of the player's active house
    /// address, or 0 if they have none.</summary>
    public int GetTownResidence(Player player) =>
        player.ActiveHouse is { } house ? _dataManager.Housing.GetAddress(house.Address)?.TownId ?? 0 : 0;

    /// <summary>Java TownService.getTownIdByPosition(Creature) — the town id of the first zone region
    /// (town_id XML attribute) containing the given position, or 0 if none.</summary>
    public int GetTownIdByPosition(Position pos)
    {
        foreach (var region in _dataManager.Zones.GetRegionsForWorld(pos.WorldId))
            if (region.TownId > 0 && region.IsInside(pos.X, pos.Y, pos.Z))
                return region.TownId;
        return 0;
    }

    /// <summary>Java Town.increasePoints, exposed at the service level (no ported "resident activity"
    /// scoring subsystem calls this yet — see this class's doc comment). Persists on level-up.</summary>
    public async Task<bool> IncreasePointsAsync(int townId, int amount, CancellationToken ct = default)
    {
        if (!_towns.TryGetValue(townId, out var town)) return false;

        bool leveledUp = town.IncreasePoints(amount);
        await _townDao.UpsertAsync(town.Id, town.Level, town.Points, town.Race, town.LevelUpDate, ct);
        return leveledUp;
    }
}

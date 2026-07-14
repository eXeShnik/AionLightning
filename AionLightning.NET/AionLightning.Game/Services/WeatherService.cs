using System.Collections.Concurrent;
using AionLightning.Commons.Services;
using AionLightning.Commons.Utils;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.World;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>
/// Java services.WeatherService — per-map, per-zone-index rolling weather. Every map with a
/// weather_table.xml entry gets one initial random weather roll at startup; <see cref="RotateAllAsync"/>
/// (armed on <see cref="WeatherOptions.RotationCron"/> by <see cref="WeatherServiceHostedService"/>)
/// re-rolls every map's weather forward, mirroring Java's day-time-change-triggered checkWeathersTime
/// (approximated here as a fixed interval — no GameTime/DayTime system is ported, see WeatherOptions).
/// The client SM_WEATHER broadcast is gated behind <see cref="WeatherOptions.SendToClients"/>; the
/// weather data itself always rotates regardless.
/// </summary>
public sealed class WeatherService
{
    private readonly ConcurrentDictionary<int, WeatherEntry[]> _byMapId = new();
    private readonly IDataManager _dataManager;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly CronService _cronService;
    private readonly IOptions<WeatherOptions> _options;
    private readonly ILogger<WeatherService> _log;

    public WeatherService(IDataManager dataManager, PlayerConnectionRegistry connRegistry,
        CronService cronService, IOptions<WeatherOptions> options, ILogger<WeatherService> log)
    {
        _dataManager = dataManager;
        _connRegistry = connRegistry;
        _cronService = cronService;
        _options = options;
        _log = log;
    }

    /// <summary>Java WeatherService() constructor — rolls an initial weather set for every mapped
    /// weather table. Called once at startup by <see cref="WeatherServiceHostedService"/>.</summary>
    public void Initialize()
    {
        foreach (var mapId in _dataManager.Weather.MapIds)
            SetNextWeather(mapId);

        _log.LogInformation("WeatherService: initialized weather for {Count} map(s)", _byMapId.Count);
    }

    /// <summary>Java WeatherService.checkWeathersTime — arms the rotation cron. A no-op if the data set
    /// is empty (nothing to rotate).</summary>
    public async Task ScheduleAsync(CancellationToken ct = default)
    {
        if (_byMapId.IsEmpty) return;
        await _cronService.Schedule(() => _ = RotateAllAsync(), _options.Value.RotationCron, longRunningTask: true);
    }

    /// <summary>Java WeatherEntry[] getWeatherEntries(mapId) — the current per-zone-index weather state
    /// for a map, or null if the map has no weather table.</summary>
    public IReadOnlyList<WeatherEntry>? GetWeatherEntries(int mapId) => _byMapId.GetValueOrDefault(mapId);

    public int GetWeatherCode(int mapId, int weatherZoneId) =>
        _byMapId.TryGetValue(mapId, out var entries)
            ? entries.FirstOrDefault(e => e.ZoneId == weatherZoneId)?.Code ?? 0
            : 0;

    /// <summary>Java WeatherService.loadWeather(Player) — sends the player's current map weather to them
    /// alone. Called on a cross-scope (world-changing) teleport by <see cref="TeleportService"/>; gated
    /// behind <see cref="WeatherOptions.SendToClients"/> (see this class's doc comment).</summary>
    public async ValueTask SendWeatherAsync(Player player, GsClientConnection conn, CancellationToken ct = default)
    {
        if (!_options.Value.SendToClients) return;
        if (GetWeatherEntries(player.Position.WorldId) is not { } entries) return;
        try { await conn.SendAsync(new SM_WEATHER(entries), ct); } catch { }
    }

    /// <summary>Java WeatherService.setNextWeather — advances every zone index of a map to its "next
    /// phase" entry (before -> mid -> after), or rolls a brand-new random weather when there is no
    /// current entry or no successor.</summary>
    private void SetNextWeather(int mapId)
    {
        var table = _dataManager.Weather.GetWeather(mapId);
        if (table is null) return;

        var current = _byMapId.GetOrAdd(mapId, _ => new WeatherEntry[table.ZoneCount]);
        for (int zoneIndex = 0; zoneIndex < current.Length; zoneIndex++)
        {
            var oldEntry = current[zoneIndex];
            var newEntry = oldEntry is null ? null : table.GetWeatherAfter(oldEntry);
            current[zoneIndex] = newEntry ?? GetRandomWeather(table, zoneIndex + 1);
        }
    }

    /// <summary>Java WeatherService.getRandomWeather — rolls a weighted rank (2 most common, 0 least),
    /// picks a random entry of that rank (falling back to its "before" phase if one exists), then applies
    /// Java's extra "don't show weather every time" rank-based suppression chance.</summary>
    private static WeatherEntry GetRandomWeather(WeatherTable table, int zoneId)
    {
        var weathers = table.GetWeathersForZone(zoneId).ToList();
        if (weathers.Count == 0) return WeatherEntry.None;

        int chance = Rnd.Get(0, 700);
        int rank = chance > 600 ? 0 : chance > 400 ? 1 : 2;

        List<WeatherEntry> chosen = [];
        while (rank >= 0)
        {
            foreach (var entry in weathers)
            {
                if (entry.Rank == -1) return entry; // constant weather
                if (entry.Rank == rank) chosen.Add(entry);
            }
            if (chosen.Count > 0) break;
            rank--;
        }

        if (chosen.Count == 0) return WeatherEntry.None;

        var picked = chosen[Rnd.Get(chosen.Count)];
        if (!picked.Before)
        {
            var beforePhase = weathers.FirstOrDefault(e => e.Name == picked.Name && e.Before);
            if (beforePhase is not null) picked = beforePhase;
        }

        chance = Rnd.Get(0, 100);
        bool suppress = picked.Rank switch
        {
            0 => chance > 33,
            1 => chance > 50,
            2 => chance > 66,
            _ => false,
        };
        return suppress ? WeatherEntry.None : picked;
    }

    /// <summary>Java WeatherService's onWeatherChange(mapId, null) broadcast branch — re-rolls and
    /// broadcasts every map's weather to its online players. Armed on <see cref="WeatherOptions.RotationCron"/>.</summary>
    private async Task RotateAllAsync()
    {
        try
        {
            foreach (var mapId in _byMapId.Keys.ToList())
            {
                SetNextWeather(mapId);
                if (!_options.Value.SendToClients) continue;
                if (GetWeatherEntries(mapId) is not { } entries) continue;

                var packet = new SM_WEATHER(entries);
                foreach (var conn in _connRegistry.GetAll())
                    if (conn.ActivePlayer is { } p && p.Position.WorldId == mapId)
                        try { await conn.SendAsync(packet); } catch { }
            }
        }
        catch (Exception e)
        {
            _log.LogError(e, "WeatherService: error rotating weather");
        }
    }
}

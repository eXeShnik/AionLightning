using AionLightning.Commons.Services;
using AionLightning.Commons.Utils;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java <c>services.DisputeLandService</c> — five daily cron windows (four rolled, one fixed)
/// toggle open-world PvP on/off for a set of contestable worlds, broadcasting the current dispute
/// status to all online players whenever it changes, and to a single player on login.
///
/// Java's <c>syncState()</c> only ever toggles <c>ZoneAttributes.PVP_ENABLED</c> on world 600030000
/// (Tiamaranta) — world 600020001 is present in <see cref="Worlds"/> (and in the broadcast packet) but
/// explicitly <c>continue</c>'d past in the toggle loop. That is ported verbatim below as a deliberate
/// quirk, not a bug to "fix": whatever made 600020001 always-PvP in Java is outside this service's scope.
///
/// The entire feature (cron scheduling, world PvP flag, login broadcast) is gated behind
/// <see cref="DisputeLandOptions.Enable"/> (default false), matching Java's own
/// <c>if (!CustomConfig.DISPUTE_ENABLED) return;</c> guards in <c>init()</c> and <c>onLogin()</c> — so
/// the byte-verified PlayerEnterWorldService login sequence is untouched unless explicitly turned on.
/// </summary>
public sealed class DisputeLandService
{
    private static readonly int[] Worlds = [600020001, 600030000];
    private const int SkippedWorldId = 600020001; // Java syncState() skip quirk — see class doc comment

    private readonly CronService _cronService;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IOptions<DisputeLandOptions> _options;
    private readonly ILogger<DisputeLandService> _log;

    private volatile bool _active;

    public DisputeLandService(CronService cronService, GameWorld world, PlayerConnectionRegistry connRegistry,
        IOptions<DisputeLandOptions> options, ILogger<DisputeLandService> log)
    {
        _cronService  = cronService;
        _world        = world;
        _connRegistry = connRegistry;
        _options      = options;
        _log          = log;
    }

    public bool IsActive => _options.Value.Enable && _active;

    /// <summary>Java DisputeLandService.init() — arms the five cron windows. No-op unless
    /// <see cref="DisputeLandOptions.Enable"/> is true.</summary>
    public async Task InitAsync()
    {
        var opt = _options.Value;
        if (!opt.Enable) return;

        bool isWeekend = DateTime.Now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        int chance = isWeekend ? opt.WeekendRandomChance : opt.RandomChance;

        await _cronService.Schedule(() => RollWindow(chance), opt.RandomSchedule, longRunningTask: true);
        await _cronService.Schedule(() => RollWindow(chance), opt.Random2Schedule, longRunningTask: true);
        await _cronService.Schedule(() => RollWindow(chance), opt.Random3Schedule, longRunningTask: true);
        await _cronService.Schedule(() => RollWindow(chance), opt.Random4Schedule, longRunningTask: true);
        await _cronService.Schedule(FixedWindow, opt.FixedSchedule, longRunningTask: true);

        _log.LogInformation("DisputeLandService: initialized (chance={Chance}%, weekend={Weekend})", chance, isWeekend);
    }

    private void RollWindow(int chancePercent)
    {
        bool activated = chancePercent > Rnd.Get(100);
        SetActive(activated);
        if (activated) ScheduleDeactivate();
    }

    private void FixedWindow()
    {
        SetActive(true);
        ScheduleDeactivate();
    }

    private void ScheduleDeactivate()
    {
        var delay = TimeSpan.FromHours(_options.Value.DisputeLandTimeHours);
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay);
            SetActive(false);
        });
    }

    private void SetActive(bool value)
    {
        _active = value;
        SyncWorldPvpFlags();
        _ = BroadcastAsync();
    }

    private void SyncWorldPvpFlags()
    {
        foreach (var worldId in Worlds)
        {
            if (worldId == SkippedWorldId) continue; // Java syncState() quirk — see class doc comment
            _world.SetWorldPvpEnabled(worldId, _active);
        }
    }

    private async Task BroadcastAsync()
    {
        var packet = new SM_DISPUTE_LAND(Worlds, _active);
        foreach (var conn in _connRegistry.GetAll())
            try { await conn.SendAsync(packet); } catch { }
    }

    /// <summary>Java DisputeLandService.onLogin — sends current dispute status to a freshly logged-in
    /// player. Gated behind <see cref="DisputeLandOptions.Enable"/> (default false).</summary>
    public async ValueTask OnLoginAsync(GsClientConnection conn, CancellationToken ct)
    {
        if (!_options.Value.Enable) return;
        try { await conn.SendAsync(new SM_DISPUTE_LAND(Worlds, _active), ct); } catch { }
    }
}

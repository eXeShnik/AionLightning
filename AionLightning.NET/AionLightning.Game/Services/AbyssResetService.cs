using AionLightning.Game.Dao;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Resets abyss daily kill/AP/GP counters at midnight UTC.
/// On Monday midnight also rotates weekly → last-week and resets weekly counters.
/// note: Java's per-rank dailyReduceGp officer/general decay (AbyssRankEnum.dailyReduceGp) is not
/// applied here — see AbyssRankService.GpThresholds comment.
/// </summary>
public sealed class AbyssResetService : BackgroundService
{
    private readonly IPlayerDao               _playerDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly ILogger<AbyssResetService> _log;

    private DateOnly _lastDailyReset = DateOnly.FromDateTime(DateTime.UtcNow);

    public AbyssResetService(IPlayerDao playerDao, PlayerConnectionRegistry connRegistry,
        ILogger<AbyssResetService> log)
    {
        _playerDao    = playerDao;
        _connRegistry = connRegistry;
        _log          = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("AbyssResetService started (checks every minute for midnight reset)");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today <= _lastDailyReset) continue;

            bool isWeeklyReset = today.DayOfWeek == DayOfWeek.Monday;
            await DoResetAsync(isWeeklyReset, ct);
            _lastDailyReset = today;
        }
    }

    private async Task DoResetAsync(bool weekly, CancellationToken ct)
    {
        _log.LogInformation("Abyss stats daily reset (weekly={Weekly})", weekly);
        try
        {
            await _playerDao.ResetAbyssDailyStatsAsync(weekly, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to persist abyss stats reset");
        }

        foreach (var conn in _connRegistry.GetAll())
        {
            var p = conn.ActivePlayer;
            if (p is null) continue;

            if (weekly)
            {
                p.AbyssLastKill   = p.AbyssWeeklyKill;
                p.AbyssLastAp     = p.AbyssWeeklyAp;
                p.AbyssLastGp     = p.AbyssWeeklyGp;
                p.AbyssWeeklyKill = 0;
                p.AbyssWeeklyAp   = 0;
                p.AbyssWeeklyGp   = 0;
            }

            p.AbyssDailyKill = 0;
            p.AbyssDailyAp   = 0;
            p.AbyssDailyGp   = 0;
        }
    }
}

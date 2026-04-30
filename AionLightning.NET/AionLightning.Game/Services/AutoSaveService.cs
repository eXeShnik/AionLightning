using AionLightning.Game.Dao;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Periodically saves all online players' state to the database so that a server
/// crash loses at most one save interval of progress rather than the entire session.
/// Saves: position, exp/level, abyss points/rank, and active quests.
/// </summary>
public sealed class AutoSaveService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IPlayerDao               _playerDao;
    private readonly IItemDao                 _itemDao;
    private readonly IQuestDao                _questDao;
    private readonly ILogger<AutoSaveService> _log;

    public AutoSaveService(
        PlayerConnectionRegistry connRegistry,
        IPlayerDao playerDao,
        IItemDao itemDao,
        IQuestDao questDao,
        ILogger<AutoSaveService> log)
    {
        _connRegistry = connRegistry;
        _playerDao    = playerDao;
        _itemDao      = itemDao;
        _questDao     = questDao;
        _log          = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("AutoSaveService started (5-minute interval)");
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            int saved = 0;
            foreach (var conn in _connRegistry.GetAll())
            {
                var player = conn.ActivePlayer;
                if (player is null) continue;

                try
                {
                    await _playerDao.UpdatePositionAsync(player.ObjectId, player.Position, ct);
                    await _playerDao.UpdateExpLevelAsync(player.ObjectId, player.Exp, player.Level, ct);
                    await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, ct);
                    await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
                    await _itemDao.SaveWarehouseAsync(player.ObjectId, player.Warehouse.All, ct);
                    await _questDao.SaveAllAsync(player.ObjectId, player.Quests.Active, ct);
                    saved++;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "AutoSave failed for player {Name}", player.Name);
                }
            }

            if (saved > 0)
                _log.LogDebug("AutoSave completed for {Count} online player(s)", saved);
        }
    }
}

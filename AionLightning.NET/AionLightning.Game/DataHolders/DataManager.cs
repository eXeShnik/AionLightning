using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class DataManager : IDataManager
{
    public PlayerStatsData   PlayerStats   { get; } = new();
    public PlayerInitialData PlayerInitial { get; } = new();

    public DataManager(ILogger<DataManager> log)
    {
        var dataRoot = Path.Combine(AppContext.BaseDirectory, "data", "static_data");
        log.LogInformation("DataManager: loading static data from {Root}", dataRoot);

        PlayerStats.Load(dataRoot, log);
        PlayerInitial.Load(dataRoot, log);

        log.LogInformation("DataManager: static data loaded");
    }
}

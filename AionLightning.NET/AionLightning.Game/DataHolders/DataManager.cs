using Microsoft.Extensions.Logging;

namespace AionLightning.Game.DataHolders;

public sealed class DataManager : IDataManager
{
    public PlayerStatsData       PlayerStats   { get; } = new();
    public PlayerInitialData     PlayerInitial { get; } = new();
    public PlayerExperienceTable ExpTable      { get; } = new();
    public SkillData             Skills        { get; } = new();
    public SkillTreeData         SkillTree     { get; } = new();
    public NpcData               Npcs          { get; } = new();
    public SpawnsData            Spawns        { get; } = new();

    public DataManager(ILogger<DataManager> log)
    {
        var dataRoot = Path.Combine(AppContext.BaseDirectory, "data", "static_data");
        log.LogInformation("DataManager: loading static data from {Root}", dataRoot);

        PlayerStats.Load(dataRoot, log);
        PlayerInitial.Load(dataRoot, log);
        ExpTable.Load(dataRoot, log);
        Skills.Load(dataRoot, log);
        SkillTree.Load(dataRoot, log);
        Npcs.Load(dataRoot, log);
        Spawns.Load(dataRoot, log);

        log.LogInformation("DataManager: static data loaded");
    }
}

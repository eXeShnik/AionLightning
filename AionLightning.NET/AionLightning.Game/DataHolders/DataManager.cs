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
    public ItemData              Items         { get; } = new();
    public ShopData              Shop          { get; } = new();
    public TeleportData          Teleports     { get; } = new();
    public RecipeData            Recipes       { get; } = new();
    public QuestData             Quests        { get; } = new();
    public QuestScriptData        QuestScripts  { get; } = new();
    public DropData              Drops         { get; } = new();
    public GatherableData        Gatherables   { get; } = new();
    public NpcSkillData                   NpcSkills   { get; } = new();
    public DecomposableSelectItemsData    SelectItems { get; } = new();
    public PlayerTitlesData               Titles      { get; } = new();
    public WalkerData                     Walkers     { get; } = new();
    public TribeData                      Tribes      { get; } = new();
    public NpcShoutData                   NpcShouts   { get; } = new();
    public PortalData                     Portals     { get; } = new();
    public BindPointData                  BindPoints  { get; } = new();
    public WorldMapData                   WorldMaps     { get; } = new();
    public GlobalDropData                 GlobalDrops   { get; } = new();
    public InstanceExitData               InstanceExits { get; } = new();
    public CubeExpanderData               CubeExpander  { get; } = new();

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
        Items.Load(dataRoot, log);
        Shop.Load(dataRoot, log);
        Teleports.Load(dataRoot, log);
        Recipes.Load(dataRoot, log);
        Quests.Load(dataRoot, log);
        QuestScripts.Load(dataRoot, log);
        Drops.Load(dataRoot, log);
        Gatherables.Load(dataRoot, log);
        NpcSkills.Load(dataRoot, log);
        SelectItems.Load(dataRoot, log);
        Titles.Load(dataRoot, log);
        Walkers.Load(dataRoot, log);
        Tribes.Load(dataRoot, log);
        NpcShouts.Load(dataRoot, log);
        Portals.Load(dataRoot, log);
        BindPoints.Load(dataRoot, log);
        WorldMaps.Load(dataRoot, log);
        GlobalDrops.Load(dataRoot, log);
        InstanceExits.Load(dataRoot, log);
        CubeExpander.Load(dataRoot, log);

        log.LogInformation("DataManager: static data loaded");
    }
}

namespace AionLightning.Game.DataHolders;

public interface IDataManager
{
    PlayerStatsData       PlayerStats   { get; }
    PlayerInitialData     PlayerInitial { get; }
    PlayerExperienceTable ExpTable      { get; }
    SkillData             Skills        { get; }
    SkillTreeData         SkillTree     { get; }
    NpcData               Npcs          { get; }
    SpawnsData            Spawns        { get; }
    ItemData              Items         { get; }
    ShopData              Shop          { get; }
    TeleportData          Teleports     { get; }
    RecipeData            Recipes       { get; }
    QuestData             Quests        { get; }
    QuestScriptData        QuestScripts  { get; }
    DropData              Drops         { get; }
    GatherableData        Gatherables   { get; }
    NpcSkillData                   NpcSkills   { get; }
    DecomposableSelectItemsData    SelectItems { get; }
    PlayerTitlesData               Titles      { get; }
    WalkerData                     Walkers     { get; }
    TribeRelationsData             TribeRelations { get; }
    NpcShoutData                   NpcShouts   { get; }
    PortalData                     Portals     { get; }
    BindPointData                  BindPoints  { get; }
    WorldMapData                   WorldMaps    { get; }
    GlobalDropData                 GlobalDrops  { get; }
    InstanceExitData               InstanceExits { get; }
    CubeExpanderData               CubeExpander  { get; }
    ZoneData                       Zones         { get; }
    PetData                        Pets          { get; }
    PetFeedData                    PetFeed       { get; }
    FlyRingData                    FlyRings      { get; }
    FlyPathData                    FlyPaths      { get; }
    HousingData                    Housing       { get; }
    HousePartsData                  HouseParts    { get; }
    HousingObjectData               HousingObjects { get; }
    SiegeLocationData               Sieges        { get; }
    SiegeSpawnData                   SiegeSpawns   { get; }
    StaticDoorData                   StaticDoors   { get; }
    RiftData                         Rifts         { get; }
    RiftSpawnData                    RiftSpawns    { get; }
    BaseData                         Bases         { get; }
    BaseSpawnData                    BaseSpawns    { get; }
    WeatherData                      Weather       { get; }
    AutoGroupData                    AutoGroups    { get; }
    VortexData                       Vortices      { get; }
    VortexSpawnData                  VortexSpawns  { get; }
    CuringObjectsData                CuringObjects { get; }
    ChallengeData                    Challenges    { get; }
    EventData                        SeasonalEvents { get; }
    RoadData                         Roads         { get; }
}

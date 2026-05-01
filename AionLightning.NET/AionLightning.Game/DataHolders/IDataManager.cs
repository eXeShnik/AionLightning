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
    DropData              Drops         { get; }
    GatherableData        Gatherables   { get; }
    NpcSkillData          NpcSkills     { get; }
}

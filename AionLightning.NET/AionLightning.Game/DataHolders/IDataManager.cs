namespace AionLightning.Game.DataHolders;

public interface IDataManager
{
    PlayerStatsData   PlayerStats   { get; }
    PlayerInitialData PlayerInitial { get; }
    SkillData         Skills        { get; }
    SkillTreeData     SkillTree     { get; }
    NpcData           Npcs          { get; }
    SpawnsData        Spawns        { get; }
}

namespace AionLightning.Game.DataHolders;

public interface IDataManager
{
    PlayerStatsData   PlayerStats   { get; }
    PlayerInitialData PlayerInitial { get; }
}

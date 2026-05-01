using AionLightning.Game.Model;

namespace AionLightning.Game.Dao;

public sealed record AbyssRankEntry(
    int    PlayerId,
    string Name,
    Race   Race,
    PlayerClass Class,
    byte   Level,
    long   AbyssPoints,
    int    AbyssRank,
    string LegionName);

public interface IPlayerDao
{
    Task<IReadOnlyList<Player>> FindByAccountIdAsync(int accountId, CancellationToken ct = default);
    Task<Player?> FindByObjectIdAsync(int objectId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<Player?> FindByNameAsync(string name, CancellationToken ct = default);
    Task<int> InsertAsync(Player player, CancellationToken ct = default);
    Task UpdatePositionAsync(int playerId, Position pos, CancellationToken ct = default);
    Task UpdateOnlineAsync(int playerId, bool online, CancellationToken ct = default);
    Task UpdateExpLevelAsync(int playerId, long exp, byte level, CancellationToken ct = default);
    Task UpdateTitleAsync(int playerId, int titleId, CancellationToken ct = default);
    Task UpdateDisplaySettingsAsync(int playerId, int display, int deny, CancellationToken ct = default);
    Task UpdateBindPointAsync(int playerId, Position? pos, CancellationToken ct = default);
    Task ResetAllOnlineAsync(CancellationToken ct = default);
    Task<int> MarkDeletedAsync(int playerId, CancellationToken ct = default);
    Task<bool> CancelDeletionAsync(int playerId, int accountId, CancellationToken ct = default);
    Task UpdateNoteAsync(int playerId, string note, CancellationToken ct = default);
    Task UpdateAbyssAsync(int playerId, long abyssPoints, int abyssRank, CancellationToken ct = default);
    Task UpdateAbyssKillStatsAsync(int playerId, int allKill, int maxRank,
        int dailyKill, long dailyAp, int weeklyKill, long weeklyAp,
        int lastKill, long lastAp, CancellationToken ct = default);
    Task<IReadOnlyList<AbyssRankEntry>> GetTopAbyssRankAsync(Race race, int limit, CancellationToken ct = default);
    Task UpdateNameAsync(int playerId, string name, CancellationToken ct = default);
    Task UpdateBonusTitleAsync(int playerId, int bonusTitleId, CancellationToken ct = default);
    Task UpdateDpAsync(int playerId, int dp, CancellationToken ct = default);
    Task UpdateSoulSicknessAsync(int playerId, int count, CancellationToken ct = default);
    Task UpdateHpMpAsync(int playerId, int currentHp, int currentMp, CancellationToken ct = default);
    Task UpdateCubeExpandAsync(int playerId, int npcExpands, CancellationToken ct = default);
    Task UpdateFpAsync(int playerId, int currentFp, CancellationToken ct = default);
    Task UpdateClassAsync(int playerId, PlayerClass newClass, CancellationToken ct = default);
}

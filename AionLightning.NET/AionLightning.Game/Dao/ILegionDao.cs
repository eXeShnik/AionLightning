using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Legion;

namespace AionLightning.Game.Dao;

public sealed record LegionRankEntry(
    int    LegionId,
    string Name,
    byte   Level,
    long   ContributionPoints,
    int    MemberCount);

public interface ILegionDao
{
    Task<int> CreateLegionAsync(string name, CancellationToken ct);
    Task<Legion?> GetLegionAsync(int legionId, CancellationToken ct);
    Task<Legion?> GetByNameAsync(string name, CancellationToken ct);
    Task<(Legion Legion, LegionMember Member)?> GetMemberLegionAsync(int playerId, CancellationToken ct);
    Task AddMemberAsync(int legionId, int playerId, string name, int classId, byte level, int worldId, LegionRank rank, CancellationToken ct);
    Task RemoveMemberAsync(int playerId, CancellationToken ct);
    Task UpdateRankAsync(int playerId, LegionRank rank, CancellationToken ct);
    Task UpdateAnnouncementAsync(int legionId, string announcement, CancellationToken ct);
    Task UpdateNameAsync(int legionId, string name, CancellationToken ct);
    Task<bool> IsNameUsedAsync(string name, CancellationToken ct);
    Task DeleteLegionAsync(int legionId, CancellationToken ct);
    Task UpdateWarehouseKinahAsync(int legionId, long kinah, CancellationToken ct);
    Task UpdateContributionPointsAsync(int legionId, long points, CancellationToken ct);
    Task UpdateLevelAsync(int legionId, int level, CancellationToken ct);
    Task<IReadOnlyList<LegionRankEntry>> GetTopLegionRankAsync(Race race, int limit, CancellationToken ct);
    Task<IReadOnlyList<Item>> FindWarehouseItemsAsync(int legionId, CancellationToken ct);
    Task SaveWarehouseItemsAsync(int legionId, IEnumerable<Item> items, CancellationToken ct);
}

using AionLightning.Game.Model.Legion;

namespace AionLightning.Game.Dao;

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
    Task DeleteLegionAsync(int legionId, CancellationToken ct);
    Task UpdateWarehouseKinahAsync(int legionId, long kinah, CancellationToken ct);
}

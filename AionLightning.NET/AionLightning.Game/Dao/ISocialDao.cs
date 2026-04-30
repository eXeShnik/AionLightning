using AionLightning.Game.Model.Social;

namespace AionLightning.Game.Dao;

public interface ISocialDao
{
    Task<IReadOnlyList<FriendEntry>> GetFriendsAsync(int playerId, CancellationToken ct = default);
    Task AddFriendAsync(int playerId, int friendId, CancellationToken ct = default);
    Task RemoveFriendAsync(int playerId, int friendId, CancellationToken ct = default);
    Task<bool> AreFriendsAsync(int playerId, int friendId, CancellationToken ct = default);

    Task<IReadOnlyList<BlockEntry>> GetBlocksAsync(int playerId, CancellationToken ct = default);
    Task AddBlockAsync(int playerId, int blockedId, string reason, CancellationToken ct = default);
    Task RemoveBlockAsync(int playerId, int blockedId, CancellationToken ct = default);
    Task UpdateBlockReasonAsync(int playerId, int blockedId, string reason, CancellationToken ct = default);
}

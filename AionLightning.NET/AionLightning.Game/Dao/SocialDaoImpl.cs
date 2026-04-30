using AionLightning.Game.Model;
using AionLightning.Game.Model.Social;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class SocialDaoImpl : ISocialDao
{
    private readonly MySqlDataSource _db;

    public SocialDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyList<FriendEntry>> GetFriendsAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<FriendRow>(
            """
            SELECT p.id, p.name, p.level, p.player_class, p.race, f.note, p.note AS player_note
            FROM friend_list f
            JOIN players p ON p.id = f.friend_id
            WHERE f.player_id = @playerId
            ORDER BY p.name
            """, new { playerId });
        return rows.Select(r => new FriendEntry(
            r.id, r.name, r.level,
            Enum.Parse<PlayerClass>(r.player_class, ignoreCase: true),
            Enum.Parse<Race>(r.race, ignoreCase: true),
            r.note, r.player_note ?? string.Empty)).ToList();
    }

    public async Task AddFriendAsync(int playerId, int friendId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "INSERT IGNORE INTO friend_list (player_id, friend_id) VALUES (@playerId, @friendId)",
            new { playerId, friendId });
    }

    public async Task RemoveFriendAsync(int playerId, int friendId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM friend_list WHERE player_id = @playerId AND friend_id = @friendId",
            new { playerId, friendId });
    }

    public async Task<bool> AreFriendsAsync(int playerId, int friendId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM friend_list WHERE player_id=@playerId AND friend_id=@friendId",
            new { playerId, friendId }) > 0;
    }

    public async Task<IReadOnlyList<BlockEntry>> GetBlocksAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<BlockRow>(
            """
            SELECT p.id, p.name, b.reason
            FROM block_list b
            JOIN players p ON p.id = b.blocked_id
            WHERE b.player_id = @playerId
            ORDER BY p.name
            """, new { playerId });
        return rows.Select(r => new BlockEntry(r.id, r.name, r.reason)).ToList();
    }

    public async Task AddBlockAsync(int playerId, int blockedId, string reason, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "INSERT IGNORE INTO block_list (player_id, blocked_id, reason) VALUES (@playerId, @blockedId, @reason)",
            new { playerId, blockedId, reason });
    }

    public async Task RemoveBlockAsync(int playerId, int blockedId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM block_list WHERE player_id=@playerId AND blocked_id=@blockedId",
            new { playerId, blockedId });
    }

    public async Task UpdateBlockReasonAsync(int playerId, int blockedId, string reason, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE block_list SET reason=@reason WHERE player_id=@playerId AND blocked_id=@blockedId",
            new { playerId, blockedId, reason });
    }

    private sealed record FriendRow(int id, string name, byte level, string player_class, string race, string note, string? player_note);
    private sealed record BlockRow(int id, string name, string reason);
}

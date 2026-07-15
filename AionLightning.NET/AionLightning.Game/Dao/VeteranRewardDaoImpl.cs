using AionLightning.Game.Model;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

/// <summary>Port of Java <c>mysql5.MySQL5VeteranRewardsDAO</c>.</summary>
public sealed class VeteranRewardDaoImpl(MySqlDataSource db) : IVeteranRewardDao
{
    public async Task<List<VeteranReward>> LoadPendingAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<VeteranRewardRow>(
            "SELECT id, player, type, item, count, kinah, sender, title, message FROM veteran_rewards ORDER BY id");
        return rows.Select(r => new VeteranReward(r.id, r.player, r.type, r.item, r.count, r.kinah, r.sender, r.title, r.message ?? string.Empty)).ToList();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM veteran_rewards WHERE id = @id", new { id });
    }

    private sealed record VeteranRewardRow(int id, string player, int type, int item, int count, int kinah, string sender, string title, string? message);
}

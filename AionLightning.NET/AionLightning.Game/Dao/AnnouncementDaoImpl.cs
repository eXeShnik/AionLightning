using AionLightning.Game.Model;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

/// <summary>Port of Java <c>mysql5.MySQL5Announcements</c>.</summary>
public sealed class AnnouncementDaoImpl(MySqlDataSource db) : IAnnouncementDao
{
    public async Task<List<Announcement>> LoadAllAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<AnnouncementRow>(
            "SELECT id, announce, faction, type, delay FROM announcements ORDER BY id");
        return rows.Select(r => new Announcement(r.id, r.announce, r.faction, r.type, r.delay)).ToList();
    }

    private sealed record AnnouncementRow(int id, string announce, string faction, string type, int delay);
}

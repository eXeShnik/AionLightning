using AionLightning.Game.Model;
using AionLightning.Game.Model.Legion;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class LegionDaoImpl : ILegionDao
{
    private readonly MySqlDataSource _db;

    public LegionDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<int> CreateLegionAsync(string name, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO legions (name) VALUES (@name);
            SELECT LAST_INSERT_ID();
            """, new { name });
    }

    public async Task<Legion?> GetLegionAsync(int legionId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<LegionRow>(
            "SELECT id, name, level, contribution_points, announcement, deputy_permission, centurion_permission, legionary_permission, volunteer_permission, warehouse_kinah FROM legions WHERE id = @legionId",
            new { legionId });
        if (row is null) return null;

        var legion = ToLegion(row);
        await LoadMembersAsync(conn, legion);
        return legion;
    }

    public async Task<Legion?> GetByNameAsync(string name, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<LegionRow>(
            "SELECT id, name, level, contribution_points, announcement, deputy_permission, centurion_permission, legionary_permission, volunteer_permission, warehouse_kinah FROM legions WHERE name = @name",
            new { name });
        if (row is null) return null;

        var legion = ToLegion(row);
        await LoadMembersAsync(conn, legion);
        return legion;
    }

    public async Task<(Legion Legion, LegionMember Member)?> GetMemberLegionAsync(int playerId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var memberRow = await conn.QuerySingleOrDefaultAsync<MemberRow>(
            """
            SELECT lm.player_id, lm.legion_id, lm.rank_id, lm.self_intro, lm.nickname,
                   p.name, p.player_class, p.level, p.world_id
            FROM legion_members lm
            JOIN players p ON p.id = lm.player_id
            WHERE lm.player_id = @playerId
            """, new { playerId });
        if (memberRow is null) return null;

        var legionRow = await conn.QuerySingleOrDefaultAsync<LegionRow>(
            "SELECT id, name, level, contribution_points, announcement, deputy_permission, centurion_permission, legionary_permission, volunteer_permission, warehouse_kinah FROM legions WHERE id = @legionId",
            new { legionId = memberRow.legion_id });
        if (legionRow is null) return null;

        var legion = ToLegion(legionRow);
        await LoadMembersAsync(conn, legion);
        var member = legion.Members.TryGetValue(playerId, out var m) ? m : ToMember(memberRow);
        return (legion, member);
    }

    public async Task AddMemberAsync(int legionId, int playerId, string name, int classId, byte level, int worldId, LegionRank rank, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "INSERT INTO legion_members (player_id, legion_id, rank_id) VALUES (@playerId, @legionId, @rankId) ON DUPLICATE KEY UPDATE legion_id=@legionId, rank_id=@rankId",
            new { playerId, legionId, rankId = (byte)rank });
    }

    public async Task RemoveMemberAsync(int playerId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM legion_members WHERE player_id = @playerId", new { playerId });
    }

    public async Task UpdateRankAsync(int playerId, LegionRank rank, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE legion_members SET rank_id = @rankId WHERE player_id = @playerId",
            new { rankId = (byte)rank, playerId });
    }

    public async Task UpdateAnnouncementAsync(int legionId, string announcement, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE legions SET announcement = @announcement WHERE id = @legionId",
            new { announcement, legionId });
    }

    public async Task UpdateNameAsync(int legionId, string name, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE legions SET name = @name WHERE id = @legionId",
            new { name, legionId });
    }

    public async Task<bool> IsNameUsedAsync(string name, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        int count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM legions WHERE name = @name", new { name });
        return count > 0;
    }

    public async Task DeleteLegionAsync(int legionId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM legions WHERE id = @legionId", new { legionId });
    }

    private static async Task LoadMembersAsync(MySqlConnection conn, Legion legion)
    {
        var rows = await conn.QueryAsync<MemberRow>(
            """
            SELECT lm.player_id, lm.legion_id, lm.rank_id, lm.self_intro, lm.nickname,
                   p.name, p.player_class, p.level, p.world_id
            FROM legion_members lm
            JOIN players p ON p.id = lm.player_id
            WHERE lm.legion_id = @legionId
            """, new { legionId = legion.LegionId });

        foreach (var r in rows)
            legion.Members[r.player_id] = ToMember(r);
    }

    private static Legion ToLegion(LegionRow r) => new()
    {
        LegionId             = r.id,
        Name                 = r.name,
        Level                = r.level,
        ContributionPoints   = r.contribution_points,
        Announcement         = r.announcement,
        DeputyPermission     = r.deputy_permission,
        CenturionPermission  = r.centurion_permission,
        LegionaryPermission  = r.legionary_permission,
        VolunteerPermission  = r.volunteer_permission,
        WarehouseKinah       = r.warehouse_kinah,
    };

    private static LegionMember ToMember(MemberRow r) => new()
    {
        ObjectId  = r.player_id,
        Name      = r.name,
        Rank      = (LegionRank)r.rank_id,
        ClassId   = r.player_class,
        Level     = r.level,
        WorldId   = r.world_id,
        SelfIntro = r.self_intro,
        Nickname  = r.nickname,
    };

    public async Task UpdateWarehouseKinahAsync(int legionId, long kinah, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE legions SET warehouse_kinah = @kinah WHERE id = @legionId",
            new { kinah, legionId });
    }

    public async Task<IReadOnlyList<LegionRankEntry>> GetTopLegionRankAsync(Race race, int limit, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(int id, string name, byte level, long contribution_points, int member_count)>(
            """
            SELECT l.id, l.name, l.level, l.contribution_points,
                   COUNT(m.player_id) AS member_count
            FROM legions l
            JOIN legion_members bg ON bg.legion_id = l.id AND bg.rank_id = 0
            JOIN players p ON p.id = bg.player_id
            LEFT JOIN legion_members m ON m.legion_id = l.id
            WHERE p.race = @raceStr
            GROUP BY l.id, l.name, l.level, l.contribution_points
            ORDER BY l.contribution_points DESC
            LIMIT @limit
            """,
            new { raceStr = race.ToString(), limit });
        return rows.Select(r => new LegionRankEntry(r.id, r.name, r.level, r.contribution_points, r.member_count))
                   .ToList();
    }

    private sealed record LegionRow(
        int id, string name, int level, long contribution_points, string announcement,
        short deputy_permission, short centurion_permission, short legionary_permission, short volunteer_permission,
        long warehouse_kinah);

    private sealed record MemberRow(
        int player_id, int legion_id, byte rank_id, string self_intro, string nickname,
        string name, int player_class, byte level, int world_id);
}

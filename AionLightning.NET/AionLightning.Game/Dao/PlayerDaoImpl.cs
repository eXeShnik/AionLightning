using AionLightning.Game.Model;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class PlayerDaoImpl : IPlayerDao
{
    private readonly MySqlDataSource _db;

    public PlayerDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<IReadOnlyList<Player>> FindByAccountIdAsync(int accountId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PlayerRow>(
            """
            SELECT id, name, account_id, exp, x, y, z, heading, world_id,
                   gender, race, player_class, creation_date, last_online, title_id, level
            FROM players
            WHERE account_id = @accountId AND deletion_date IS NULL
            ORDER BY id
            """, new { accountId });
        return rows.Select(ToPlayer).ToList();
    }

    public async Task<Player?> FindByObjectIdAsync(int objectId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<PlayerRow>(
            """
            SELECT id, name, account_id, exp, x, y, z, heading, world_id,
                   gender, race, player_class, creation_date, last_online, title_id, level
            FROM players
            WHERE id = @objectId AND deletion_date IS NULL
            """, new { objectId });
        return row is null ? null : ToPlayer(row);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM players WHERE name = @name", new { name }) > 0;
    }

    public async Task<int> InsertAsync(Player player, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO players (id, name, account_id, account_name, exp, x, y, z, heading, world_id,
                                 gender, race, player_class, creation_date, level)
            VALUES (@ObjectId, @Name, @AccountId, @AccountName, @Exp,
                    @X, @Y, @Z, @Heading, @WorldId,
                    @GenderStr, @RaceStr, @ClassStr, @CreationDate, @Level);
            SELECT LAST_INSERT_ID();
            """,
            new
            {
                player.ObjectId,
                player.Name,
                player.AccountId,
                AccountName = "",
                player.Exp,
                X = player.Position.X,
                Y = player.Position.Y,
                Z = player.Position.Z,
                Heading = player.Position.Heading,
                WorldId = player.Position.WorldId,
                GenderStr = player.Gender.ToString(),
                RaceStr = player.Race.ToString(),
                ClassStr = player.PlayerClass.ToString(),
                player.CreationDate,
                Level = player.Level,
            });
    }

    public async Task UpdatePositionAsync(int playerId, Position pos, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET x=@X, y=@Y, z=@Z, heading=@Heading, world_id=@WorldId WHERE id=@playerId",
            new { pos.X, pos.Y, pos.Z, pos.Heading, pos.WorldId, playerId });
    }

    public async Task UpdateOnlineAsync(int playerId, bool online, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET online=@online WHERE id=@playerId",
            new { online = online ? 1 : 0, playerId });
    }

    private static Player ToPlayer(PlayerRow r) => new()
    {
        ObjectId      = r.id,
        Name          = r.name,
        AccountId     = r.account_id,
        Exp           = r.exp,
        Position      = new Position(r.x, r.y, r.z, r.heading, r.world_id),
        Gender        = Enum.Parse<Gender>(r.gender, ignoreCase: true),
        Race          = Enum.Parse<Race>(r.race, ignoreCase: true),
        PlayerClass   = Enum.Parse<PlayerClass>(r.player_class, ignoreCase: true),
        CreationDate  = r.creation_date ?? DateTime.UtcNow,
        LastOnline    = r.last_online,
        TitleId       = r.title_id,
        Level         = r.level,
    };

    private sealed record PlayerRow(
        int id, string name, int account_id, long exp,
        float x, float y, float z, int heading, int world_id,
        string gender, string race, string player_class,
        DateTime? creation_date, DateTime? last_online,
        int title_id, byte level);
}

using AionLightning.Game.Model;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class PlayerDaoImpl : IPlayerDao
{
    private readonly MySqlDataSource _db;

    public PlayerDaoImpl(MySqlDataSource db) => _db = db;

    private const string SelectColumns =
        """
        id, name, account_id, exp, x, y, z, heading, world_id,
        gender, race, player_class, creation_date, last_online, title_id, display_settings, deny_settings,
        level, deletion_date, bind_x, bind_y, bind_z, bind_world_id, note, abyss_points, abyss_rank,
        bonus_title_id, dp, soul_sickness, current_hp, current_mp, npc_expands
        """;

    public async Task<IReadOnlyList<Player>> FindByAccountIdAsync(int accountId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PlayerRow>(
            $"SELECT {SelectColumns} FROM players WHERE account_id = @accountId ORDER BY id",
            new { accountId });
        return rows.Select(ToPlayer).ToList();
    }

    public async Task<Player?> FindByObjectIdAsync(int objectId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<PlayerRow>(
            $"SELECT {SelectColumns} FROM players WHERE id = @objectId AND deletion_date IS NULL",
            new { objectId });
        return row is null ? null : ToPlayer(row);
    }

    public async Task<Player?> FindByNameAsync(string name, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<PlayerRow>(
            $"SELECT {SelectColumns} FROM players WHERE name = @name AND deletion_date IS NULL",
            new { name });
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
            INSERT INTO players (name, account_id, account_name, exp, x, y, z, heading, world_id,
                                 gender, race, player_class, creation_date, level,
                                 display_settings, deny_settings)
            VALUES (@Name, @AccountId, @AccountName, @Exp,
                    @X, @Y, @Z, @Heading, @WorldId,
                    @GenderStr, @RaceStr, @ClassStr, @CreationDate, @Level,
                    0, 0);
            SELECT LAST_INSERT_ID();
            """,
            new
            {
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

    public async Task UpdateExpLevelAsync(int playerId, long exp, byte level, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET exp=@exp, level=@level WHERE id=@playerId",
            new { exp, level, playerId });
    }

    public async Task UpdateBindPointAsync(int playerId, Position? pos, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        if (pos is null)
        {
            await conn.ExecuteAsync(
                "UPDATE players SET bind_x=NULL, bind_y=NULL, bind_z=NULL, bind_world_id=NULL WHERE id=@playerId",
                new { playerId });
        }
        else
        {
            await conn.ExecuteAsync(
                "UPDATE players SET bind_x=@X, bind_y=@Y, bind_z=@Z, bind_world_id=@WorldId WHERE id=@playerId",
                new { pos.Value.X, pos.Value.Y, pos.Value.Z, pos.Value.WorldId, playerId });
        }
    }

    public async Task UpdateTitleAsync(int playerId, int titleId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET title_id=@titleId WHERE id=@playerId",
            new { titleId, playerId });
    }

    public async Task UpdateDisplaySettingsAsync(int playerId, int display, int deny, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET display_settings=@display, deny_settings=@deny WHERE id=@playerId",
            new { display, deny, playerId });
    }

    public async Task ResetAllOnlineAsync(CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("UPDATE players SET online=0");
    }

    public async Task<int> MarkDeletedAsync(int playerId, CancellationToken ct = default)
    {
        var deletionDate = DateTime.UtcNow.AddDays(7);
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET deletion_date=@deletionDate WHERE id=@playerId",
            new { deletionDate, playerId });
        return (int)new DateTimeOffset(deletionDate).ToUnixTimeSeconds();
    }

    public async Task<bool> CancelDeletionAsync(int playerId, int accountId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        int rows = await conn.ExecuteAsync(
            "UPDATE players SET deletion_date=NULL WHERE id=@playerId AND account_id=@accountId AND deletion_date IS NOT NULL",
            new { playerId, accountId });
        return rows > 0;
    }

    public async Task UpdateNoteAsync(int playerId, string note, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("UPDATE players SET note=@note WHERE id=@playerId", new { playerId, note });
    }

    public async Task UpdateAbyssAsync(int playerId, long abyssPoints, int abyssRank, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET abyss_points=@abyssPoints, abyss_rank=@abyssRank WHERE id=@playerId",
            new { playerId, abyssPoints, abyssRank });
    }

    public async Task UpdateNameAsync(int playerId, string name, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("UPDATE players SET name=@name WHERE id=@playerId", new { playerId, name });
    }

    public async Task UpdateBonusTitleAsync(int playerId, int bonusTitleId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET bonus_title_id=@bonusTitleId WHERE id=@playerId",
            new { bonusTitleId, playerId });
    }

    public async Task<IReadOnlyList<AbyssRankEntry>> GetTopAbyssRankAsync(Race race, int limit, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(int id, string name, string race, string player_class, byte level, long abyss_points, int abyss_rank)>(
            """
            SELECT id, name, race, player_class, level, abyss_points, abyss_rank
            FROM players
            WHERE race = @raceStr AND deletion_date IS NULL
            ORDER BY abyss_points DESC
            LIMIT @limit
            """,
            new { raceStr = race.ToString(), limit });
        return rows.Select(r => new AbyssRankEntry(
            r.id,
            r.name,
            Enum.Parse<Race>(r.race, ignoreCase: true),
            Enum.Parse<PlayerClass>(r.player_class, ignoreCase: true),
            r.level,
            r.abyss_points,
            r.abyss_rank > 0 ? r.abyss_rank : 1,
            string.Empty)).ToList();
    }

    private static Player ToPlayer(PlayerRow r)
    {
        var player = new Player
        {
            ObjectId     = r.id,
            Name         = r.name,
            AccountId    = r.account_id,
            Exp          = r.exp,
            Position     = new Position(r.x, r.y, r.z, r.heading, r.world_id),
            Gender       = Enum.Parse<Gender>(r.gender, ignoreCase: true),
            Race         = Enum.Parse<Race>(r.race, ignoreCase: true),
            PlayerClass  = Enum.Parse<PlayerClass>(r.player_class, ignoreCase: true),
            CreationDate = r.creation_date ?? DateTime.UtcNow,
            LastOnline   = r.last_online,
            TitleId          = r.title_id,
            DisplaySettings  = r.display_settings,
            DenySettings     = r.deny_settings,
            Level            = r.level,
            DeletionDate     = r.deletion_date,
            Note             = r.note ?? string.Empty,
            AbyssPoints      = r.abyss_points,
            AbyssRank        = r.abyss_rank > 0 ? r.abyss_rank : 1,
            BonusTitleId     = r.bonus_title_id,
            Dp                  = r.dp,
            SoulSicknessCount   = r.soul_sickness,
            CurrentHp           = r.current_hp ?? 0,
            CurrentMp           = r.current_mp ?? 0,
            NpcExpands          = r.npc_expands,
        };

        if (r.bind_x.HasValue && r.bind_y.HasValue && r.bind_z.HasValue && r.bind_world_id.HasValue)
            player.BindPosition = new Position(r.bind_x.Value, r.bind_y.Value, r.bind_z.Value, 0, r.bind_world_id.Value);

        return player;
    }

    public async Task UpdateDpAsync(int playerId, int dp, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("UPDATE players SET dp=@dp WHERE id=@playerId", new { dp, playerId });
    }

    public async Task UpdateSoulSicknessAsync(int playerId, int count, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("UPDATE players SET soul_sickness=@count WHERE id=@playerId", new { count, playerId });
    }

    public async Task UpdateHpMpAsync(int playerId, int currentHp, int currentMp, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET current_hp=@currentHp, current_mp=@currentMp WHERE id=@playerId",
            new { currentHp, currentMp, playerId });
    }

    public async Task UpdateCubeExpandAsync(int playerId, int npcExpands, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE players SET npc_expands=@npcExpands WHERE id=@playerId",
            new { npcExpands, playerId });
    }

    private sealed record PlayerRow(
        int id, string name, int account_id, long exp,
        float x, float y, float z, int heading, int world_id,
        string gender, string race, string player_class,
        DateTime? creation_date, DateTime? last_online,
        int title_id, short display_settings, short deny_settings,
        byte level, DateTime? deletion_date,
        float? bind_x, float? bind_y, float? bind_z, int? bind_world_id,
        string? note, long abyss_points, int abyss_rank,
        int bonus_title_id, int dp, int soul_sickness, int? current_hp, int? current_mp, int npc_expands);
}

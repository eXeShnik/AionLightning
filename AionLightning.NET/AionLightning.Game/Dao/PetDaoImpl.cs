using AionLightning.Game.Model.Pet;
using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class PetDaoImpl : IPetDao
{
    private readonly MySqlDataSource _db;

    public PetDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<List<PetCommonData>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PetRow>(
            """
            SELECT pet_id, decoration, name, birthday, despawn_time, expire_time,
                   hungry_level, feed_progress, reuse_time, dopings,
                   mood_started, counter, mood_cd_started, gift_cd_started
            FROM player_pets WHERE player_id = @playerId
            """, new { playerId });
        return rows.Select(r => ToCommonData(r, playerId)).ToList();
    }

    public async Task InsertAsync(int playerId, PetCommonData pet, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO player_pets (player_id, pet_id, decoration, name, birthday, despawn_time, expire_time,
                hungry_level, feed_progress, reuse_time, dopings, mood_started, counter, mood_cd_started, gift_cd_started)
            VALUES (@playerId, @PetId, @Decoration, @Name, @Birthday, @DespawnTime, @ExpireTime,
                @HungryLevel, @FeedProgress, @ReuseTime, @Dopings, @MoodStarted, @Counter, @MoodCdStarted, @GiftCdStarted)
            """, new
            {
                playerId,
                pet.PetId,
                pet.Decoration,
                pet.Name,
                Birthday = ToUnixMs(pet.Birthday),
                DespawnTime = ToUnixMsOrNull(pet.DespawnTime),
                ExpireTime = ToUnixMsOrNull(pet.ExpireTime),
                pet.HungryLevel,
                pet.FeedProgress,
                pet.ReuseTime,
                pet.Dopings,
                pet.MoodStarted,
                pet.Counter,
                pet.MoodCdStarted,
                pet.GiftCdStarted,
            });
    }

    public async Task RemoveAsync(int playerId, int petId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "DELETE FROM player_pets WHERE player_id = @playerId AND pet_id = @petId",
            new { playerId, petId });
    }

    public async Task UpdateNameAsync(int playerId, int petId, string name, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE player_pets SET name = @name WHERE player_id = @playerId AND pet_id = @petId",
            new { playerId, petId, name });
    }

    private static long ToUnixMs(DateTime dt) => new DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeMilliseconds();

    private static long? ToUnixMsOrNull(DateTime? dt) => dt.HasValue ? ToUnixMs(dt.Value) : null;

    private static DateTime FromUnixMs(long ms) => DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;

    private static DateTime? FromUnixMsOrNull(long? ms) => ms.HasValue ? FromUnixMs(ms.Value) : null;

    private static PetCommonData ToCommonData(PetRow r, int playerId) => new()
    {
        PetId = r.pet_id,
        MasterObjectId = playerId,
        Decoration = r.decoration,
        Name = r.name,
        Birthday = FromUnixMs(r.birthday),
        DespawnTime = FromUnixMsOrNull(r.despawn_time),
        ExpireTime = FromUnixMsOrNull(r.expire_time),
        HungryLevel = r.hungry_level,
        FeedProgress = r.feed_progress,
        ReuseTime = r.reuse_time,
        Dopings = r.dopings,
        MoodStarted = r.mood_started,
        Counter = r.counter,
        MoodCdStarted = r.mood_cd_started,
        GiftCdStarted = r.gift_cd_started,
    };

    private sealed record PetRow(
        int pet_id, int decoration, string name, long birthday, long? despawn_time, long? expire_time,
        int hungry_level, int feed_progress, long reuse_time, string dopings,
        long mood_started, int counter, long mood_cd_started, long gift_cd_started);
}

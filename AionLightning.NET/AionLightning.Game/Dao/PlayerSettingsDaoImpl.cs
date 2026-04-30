using Dapper;
using MySqlConnector;

namespace AionLightning.Game.Dao;

public sealed class PlayerSettingsDaoImpl : IPlayerSettingsDao
{
    private readonly MySqlDataSource _db;

    public PlayerSettingsDaoImpl(MySqlDataSource db) => _db = db;

    public async Task<(byte[]? UiSettings, byte[]? Shortcuts, byte[]? HouseBuddies)> LoadAsync(int playerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(byte SettingsType, byte[] Settings)>(
            "SELECT settings_type, settings FROM player_settings WHERE player_id = @playerId",
            new { playerId });

        byte[]? ui = null, shortcuts = null, houseBuddies = null;
        foreach (var (type, data) in rows)
        {
            switch (type)
            {
                case 0: ui          = data; break;
                case 1: shortcuts   = data; break;
                case 2: houseBuddies= data; break;
            }
        }
        return (ui, shortcuts, houseBuddies);
    }

    public async Task SaveSettingAsync(int playerId, byte settingsType, byte[] data, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "REPLACE INTO player_settings (player_id, settings_type, settings) VALUES (@playerId, @settingsType, @data)",
            new { playerId, settingsType, data });
    }
}

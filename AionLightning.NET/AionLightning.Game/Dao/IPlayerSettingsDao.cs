namespace AionLightning.Game.Dao;

public interface IPlayerSettingsDao
{
    Task<(byte[]? UiSettings, byte[]? Shortcuts, byte[]? HouseBuddies)> LoadAsync(int playerId, CancellationToken ct = default);
    Task SaveSettingAsync(int playerId, byte settingsType, byte[] data, CancellationToken ct = default);
}

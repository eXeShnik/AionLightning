namespace AionLightning.Game.Model.House;

/// <summary>
/// Java model.house.PlayerScript — one decoration-script slot's compressed data. The default state
/// (<see cref="CompressedBytes"/> null, <see cref="UncompressedSize"/> -1) means the slot is empty. Java
/// guards concurrent reads/writes with AbstractLockManager read/write locks; dropped here because house
/// scripts are only ever touched from the single-threaded per-connection packet loop (CM_HOUSE_SCRIPT) or
/// the startup load sweep (HousingService.LoadHouseScriptsAsync), never both at once.
/// </summary>
public sealed class PlayerScript
{
    public int UncompressedSize { get; private set; } = -1;
    public byte[]? CompressedBytes { get; private set; }

    public void SetData(byte[]? compressedBytes, int uncompressedSize)
    {
        CompressedBytes = compressedBytes;
        UncompressedSize = uncompressedSize;
    }
}

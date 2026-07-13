using AionLightning.Game.Model.House;

namespace AionLightning.Game.Dao;

public interface IHouseDao
{
    Task<List<House>> LoadAllAsync(CancellationToken ct = default);
    Task<House?> LoadByOwnerAsync(int playerObjectId, CancellationToken ct = default);
    Task StoreAsync(House house, CancellationToken ct = default);
    Task DeleteByOwnerAsync(int playerObjectId, CancellationToken ct = default);
}

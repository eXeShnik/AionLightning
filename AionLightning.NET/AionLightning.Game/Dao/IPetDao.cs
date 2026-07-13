using AionLightning.Game.Model.Pet;

namespace AionLightning.Game.Dao;

public interface IPetDao
{
    Task<List<PetCommonData>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default);
    Task InsertAsync(int playerId, PetCommonData pet, CancellationToken ct = default);
    Task RemoveAsync(int playerId, int petId, CancellationToken ct = default);
    Task UpdateNameAsync(int playerId, int petId, string name, CancellationToken ct = default);
}

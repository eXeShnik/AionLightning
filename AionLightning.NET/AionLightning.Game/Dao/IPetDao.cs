using AionLightning.Game.Model.Pet;

namespace AionLightning.Game.Dao;

public interface IPetDao
{
    Task<List<PetCommonData>> LoadByPlayerIdAsync(int playerId, CancellationToken ct = default);
    Task InsertAsync(int playerId, PetCommonData pet, CancellationToken ct = default);
    Task RemoveAsync(int playerId, int petId, CancellationToken ct = default);
    Task UpdateNameAsync(int playerId, int petId, string name, CancellationToken ct = default);

    /// <summary>Java PlayerPetsDAO.saveFeedStatus — persists the packed feed progress on pet dismiss.</summary>
    Task SaveFeedStatusAsync(int playerId, int petId, int hungryLevel, int feedProgress, long reuseTime, CancellationToken ct = default);

    /// <summary>Java PlayerPetsDAO.setTime — persists just the refeed cooldown (full-feed reward path).</summary>
    Task SetRefeedTimeAsync(int playerId, int petId, long reuseTime, CancellationToken ct = default);

    /// <summary>Java PlayerPetsDAO.savePetMoodData — persists mood/shuggle/gift state on pet dismiss.</summary>
    Task SaveMoodDataAsync(int playerId, int petId, long moodStarted, int counter, long moodCdStarted,
        long giftCdStarted, DateTime? despawnTime, CancellationToken ct = default);
}

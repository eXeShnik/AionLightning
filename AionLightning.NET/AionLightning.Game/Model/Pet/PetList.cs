namespace AionLightning.Game.Model.Pet;

/// <summary>Per-player collection of adopted pets, keyed by pet_id (Java model.gameobjects.player.PetList).</summary>
public sealed class PetList
{
    private readonly Dictionary<int, PetCommonData> _pets = new();

    /// <summary>pet_id of the pet with the most recent despawn time — the one the client resumes on login.</summary>
    public int? LastUsedPetId { get; set; }

    public IReadOnlyCollection<PetCommonData> All => _pets.Values;

    public void Add(PetCommonData pet) => _pets[pet.PetId] = pet;

    public bool Remove(int petId) => _pets.Remove(petId);

    public PetCommonData? Get(int petId) => _pets.GetValueOrDefault(petId);

    public bool Has(int petId) => _pets.ContainsKey(petId);
}

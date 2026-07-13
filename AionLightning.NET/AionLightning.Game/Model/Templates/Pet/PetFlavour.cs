namespace AionLightning.Game.Model.Templates.Pet;

/// <summary>One &lt;flavour&gt; block from pet_feed.xml (Java model.templates.pet.PetFlavour) — the feed
/// behaviour bound to a pet's FOOD function id: how many regular feeds fill the pet up
/// (<see cref="FullCount"/>), the loved-food cap (<see cref="LovedFoodLimit"/>), the post-full refeed
/// cooldown in minutes (<see cref="CooldownMinutes"/>), and the reward groups themselves.</summary>
public sealed record PetFlavour(int Id, int FullCount, int LovedFoodLimit, int CooldownMinutes, List<PetRewards> Food);

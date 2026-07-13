namespace AionLightning.Game.Model.Templates.Pet;

/// <summary>One &lt;food group="..."&gt; reward group from pet_feed.xml (Java model.templates.pet.PetRewards).
/// <see cref="Loved"/> groups feed a separate, smaller counter (<see cref="PetFeedProgress.LovedFoodRemaining"/>)
/// instead of the regular full-count progression.</summary>
public sealed record PetRewards(FoodType Type, bool Loved, List<PetFeedResult> Results);

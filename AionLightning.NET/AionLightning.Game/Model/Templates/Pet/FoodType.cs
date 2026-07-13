namespace AionLightning.Game.Model.Templates.Pet;

/// <summary>Pet-feed item group (Java model.templates.pet.FoodType). EXCLUDES/STINKY are exclusion-only
/// groups (never a valid reward-group match); MISCELLANEOUS is a synthetic aggregate resolved against
/// ARMOR|BALAUR_SCALES|BONES|FLUIDS|SOULS|THORNS — see <see cref="DataHolders.PetFeedData.IsFood"/>.</summary>
public enum FoodType
{
    AETHER_CRYSTAL_BISCUIT,
    AETHER_GEM_BISCUIT,
    AETHER_POWDER_BISCUIT,
    ARMOR,
    BALAUR_SCALES,
    BONES,
    EXCLUDES,
    FLUIDS,
    HEALTHY_FOOD_ALL,
    HEALTHY_FOOD_SPICY,
    MISCELLANEOUS,
    POPPY_SNACK,
    POPPY_SNACK_TASTY,
    POPPY_SNACK_NUTRITIOUS,
    SOULS,
    SHUGO_EVENT_COIN,
    STINKY,
    THORNS,
}
